using Discord;
using Discord.Audio;
using Discord.WebSocket;
using ElevenLabsTTSDiscordBot.DataModels;
using System.Diagnostics;

namespace ElevenLabsTTSDiscordBot
{
    internal class QueueData
    {
        public string voice { get; set; }
        public string text { get; set; } 
        public ISocketMessageChannel channel { get; set; }
        public string fileLoc { get; set; }

        public QueueData(string voice, string text, string fileLoc, ISocketMessageChannel channel)
        {
            this.voice = voice;
            this.text = text;
            this.fileLoc = fileLoc;
            this.channel = channel;
        }
    }

    public class ServerConnectionData
    {
        private IGuild _server;
        private TTSService _ttsService;
        private Dictionary<string, List<VoiceDataObj>> apiKeysAndVoices = new Dictionary<string, List<VoiceDataObj>>();
        public ServerConnectionData(IGuild server, string apiKey, TTSService ttsService)
        {
            _server = server;
            _ttsService = ttsService;
            var _ = Task.FromResult(AddApiKey(apiKey));
        }

        private IAudioClient? audioClient;
        private IVoiceChannel? vc;
        private AudioOutStream? discord;
        private Queue<QueueData> filesToPlay { get; set; } = new Queue<QueueData>();

        private bool IsConnected
        {
            get => audioClient != null && audioClient.ConnectionState == ConnectionState.Connected;
        }

        public async Task RefreshVoicesAndCommands()
        {
            var voices = await _ttsService.GetVoices(apiKeysAndVoices.Keys.ToList());
            foreach (var voice in voices)
            {
                if (apiKeysAndVoices.ContainsKey(voice.Key))
                {
                    apiKeysAndVoices[voice.Key].Clear();
                    apiKeysAndVoices[voice.Key].AddRange(voice.Value.Select(v => new VoiceDataObj(v.name, v.voice_id)));
                }
            }

            var optionBuilder = new SlashCommandOptionBuilder()
                .WithName("speaker")
                .WithDescription("The voice you'd like to use")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String);
                
            foreach(var _voices in apiKeysAndVoices.Values)
            {
                foreach(var voice in _voices)
                {
                    optionBuilder.AddChoice(voice.VoiceName, voice.APIVoiceId);
                }
            }

            var aiCommand = new SlashCommandBuilder()
            .WithName("ai")
            .WithDescription("Generates an audio file of the selected individual saying the sent text.")
            .AddOption(optionBuilder)
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("text")
                .WithDescription("The text to be read aloud.")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.String)
            );
            await _server.CreateApplicationCommandAsync(aiCommand.Build());
        }

        public async Task AddApiKey(string apiKey)
        {
            if (!apiKeysAndVoices.ContainsKey(apiKey))
                apiKeysAndVoices.Add(apiKey, new List<VoiceDataObj>());

            await RefreshVoicesAndCommands();
        }

        public async Task RemoveApiKey(string apiKey)
        {
            if (apiKeysAndVoices.ContainsKey(apiKey))
                apiKeysAndVoices.Remove(apiKey);

            await RefreshVoicesAndCommands();
        }

        public async Task ClearApiKeys()
        {
            apiKeysAndVoices.Clear();
            await RefreshVoicesAndCommands();
            await Disconnect();
        }

        public async Task GenerateVoiceLine(string voiceId, string voiceLine, ulong? serverId, SocketSlashCommand command)
        {
            if (!serverId.HasValue)
            {
                await command.RespondAsync("Server Id could not be established. Please contact an admin");
                return;
            }

            var voices = apiKeysAndVoices.FirstOrDefault(v => v.Value.Any(voice => voice.APIVoiceId == voiceId));

            if (string.IsNullOrEmpty(voices.Key))
            {
                await command.RespondAsync("Voice could not be found amongst provided API keys. Please contact an admin.");
                return;
            }

            var apiKey = voices.Key;
            var voiceName = voices.Value.First(voice => voice.APIVoiceId == voiceId).VoiceName;

            string filePath = await _ttsService.GetVoiceFileAsync(apiKey, serverId.Value, voiceId, voiceLine);

            QueueData data = new(voiceName, voiceLine, filePath, command.Channel);

            if (IsConnected)
                _ = Task.FromResult(StreamFromFile(data));
            else
                _ = Task.FromResult(SendSoundFileAsReply(data));
        }

        private async Task SendSoundFileAsReply(QueueData data)
        {
            using (FileStream fs = File.OpenRead(data.fileLoc))
            {
                await data.channel.SendFileAsync(
                    fs,
                    data.voice + "_" + DateTime.Now.ToString("MM/dd/yy_H:mm:ss") + ".mp3",
                    text: $"{data.voice}: \"{data.text}\""
                );
            }

            File.Delete(data.fileLoc);
        }

        private async Task StreamFromFile(QueueData data)
        {
            if (filesToPlay.Count != 0) { 
                filesToPlay.Enqueue(data);
            }
            else
            {
                filesToPlay.Enqueue(data);
                while (filesToPlay.Count > 0)
                {
                    if (IsConnected && discord != null)
                    {
                        using (var ffmpeg = CreateStream(filesToPlay.Peek().fileLoc))
                        using (var output = ffmpeg.StandardOutput.BaseStream)
                        {
                            try
                            {
                                await output.CopyToAsync(discord);
                            }
                            finally { await discord.FlushAsync(); }
                        }
                    }
                    _ = Task.FromResult(SendSoundFileAsReply(filesToPlay.Dequeue()));
                }
            }
        }

        public async Task Connect(IVoiceChannel vc)
        {
            try
            {
                this.vc = vc;
                audioClient = await vc.ConnectAsync();
                discord = audioClient.CreatePCMStream(AudioApplication.Voice);
                filesToPlay.Clear();
                var folder = new DirectoryInfo(BotData.VoiceFileLoc + _server.Id);
                if (folder.Exists)
                    foreach (var file in folder.GetFiles())
                    {
                        file.Delete();
                    }
            }
            catch(Exception ex)
            {

            }
            
        }

        public async Task Disconnect()
        {
            if (vc != null)
            {
                await vc.DisconnectAsync();
                audioClient = null;
                discord = null;
                filesToPlay.Clear();
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true
            }) ?? throw new NotImplementedException("Process could not be started");
        }
    }
}
