using ElevenLabsTTSDiscordBot.DataModels;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace ElevenLabsTTSDiscordBot
{
    public class TTSService
    {
        
        private Dictionary<ulong, int> fileIds { get; set; }
        private static readonly HttpClient _http = new HttpClient();
        
        public TTSService()
        {
            fileIds = new Dictionary<ulong, int>();
        }

        public async Task<string> GetVoiceFileAsync(string apiKey, ulong serverId, string voiceId, string text)
        {
            var history = await GetVoiceHistory(apiKey);

            if (history.Any(
                    h => h.text == text && 
                    h.voice_id == voiceId))
            {
                return await GetPreMadeSoundFile(apiKey, serverId, history.First(h => h.text == text && h.voice_id == voiceId).history_item_id);
            }
            else
            {
                return await GetVoiceStreamAsync(apiKey, serverId, voiceId, text);
            }
        }

        private async Task<List<VoiceHistoryData>> GetVoiceHistory(string apiKey)
        {
            
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.elevenlabs.io/v1/history"),
                Headers =
                {
                    { "xi-api-key", apiKey }
                }
            };
            HttpResponseMessage response = await _http.SendAsync(httpRequestMessage);
            
            var obj = JsonSerializer.Deserialize<VoiceHistory>(await response.Content.ReadAsStringAsync());
            return obj?.history ?? new List<VoiceHistoryData>();
        }

        private async Task<string> GetVoiceStreamAsync(string apiKey, ulong serverId, string voiceId, string text)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://api.elevenlabs.io/v1/text-to-speech/{voiceId}"),
                Headers =
                {
                    { "xi-api-key", apiKey }
                },
                Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    text = text,
                    voice_settings = new
                    {
                        stability = 0.75,
                        similarity_boost = 0.75
                    }
                }), UTF8Encoding.UTF8, "application/json")
            };

            HttpResponseMessage response = await _http.SendAsync(httpRequestMessage);
            string fileDir = BotData.VoiceFileLoc + GetFileName(serverId);
            var fileStream = File.Create(fileDir);

            var stream = await response.Content.ReadAsStreamAsync();
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            return fileDir;
        }

        private async Task<string> GetPreMadeSoundFile(string apiKey, ulong serverId, string fileId)
        {
            HttpRequestMessage httpRequestMessage = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://api.elevenlabs.io/v1/history/{fileId}/audio"),
                Headers =
                {
                    { "xi-api-key", apiKey }
                }
            };
            HttpResponseMessage response = await _http.SendAsync(httpRequestMessage);
            string fileDir = BotData.VoiceFileLoc + GetFileName(serverId);
            var fileStream = File.Create(fileDir);

            var stream = await response.Content.ReadAsStreamAsync();
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(fileStream);
            fileStream.Close();
            return fileDir;
        }

        private string GetFileName(ulong serverId)
        {
            if (!fileIds.ContainsKey(serverId))
            {
                fileIds.Add(serverId, 1);
            }

            Directory.CreateDirectory(BotData.VoiceFileLoc + serverId);

            return $"{serverId}\\voice_{fileIds[serverId]++}.mpeg";
        }

        public async Task<Dictionary<string, IEnumerable<VoiceObj>>> GetVoices(List<string> apiKeys)
        {
            Dictionary<string, IEnumerable<VoiceObj>> voicesToReturn = new Dictionary<string, IEnumerable<VoiceObj>>();
            foreach (string apiKey in apiKeys)
            {
                HttpRequestMessage httpRequestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Get,
                    RequestUri = new Uri($"https://api.elevenlabs.io/v1/voices"),
                    Headers =
                    {
                        { "xi-api-key", apiKey }
                    }
                };
                HttpResponseMessage response = await _http.SendAsync(httpRequestMessage);
                var obj = JsonSerializer.Deserialize<Voices>(await response.Content.ReadAsStringAsync());
                voicesToReturn.Add(apiKey, obj?.voices.Where(v => v.category == "cloned") ?? new List<VoiceObj>());
            }
            
            return voicesToReturn;
        }
    }
}
