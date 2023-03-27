using Discord;
using Discord.Net;
using Discord.WebSocket;
using ElevenLabsTTSDiscordBot;
using ElevenLabsTTSDiscordBot.DataModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Windows.Input;

internal class Program
{
    private readonly IServiceProvider _serviceProvider;
    public Program()
    {
        _serviceProvider = CreateProvider();
    }

    private static void Main(string[] args) 
        => new Program().MainAsync().GetAwaiter().GetResult();

    public async Task MainAsync()
    {
        BuildDataModels();

        var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

        client.Log += Log;
        client.Ready += Client_Ready;
        client.SlashCommandExecuted += SlashCommandHandler;

        await client.LoginAsync(TokenType.Bot, BotData.Token);

        await client.StartAsync();
        
        await Task.Delay(-1);
    }

    private async Task SlashCommandHandler(SocketSlashCommand command)
    {
        if (command.GuildId == null)
        {
            await command.RespondAsync($"Commands only available from Servers");
            return;
        }
        switch (command.Data.Name)
        {
            case "ai":
                await HandleTTSCommand(command);
                break;
            case "join":
                await HandleJoinCommand(command);
                break;
            case "leave":
                await HandleLeaveCommand(command);
                break;
            case "addkey":
                await HandleAddKeyCommand(command);
                break;
            case "refresh":
                await HandleRefreshCommand(command);
                break;
            case "removekey":
                await HandleRemoveKeyCommand(command);
                break;
            case "clearkeys":
                await HandleClearKeysCommand(command);
                break;
            default:
                await command.RespondAsync($"Command not recognized...");
                break;
        }
    }

    private async Task HandleAddKeyCommand(SocketSlashCommand command)
    {
        await command.RespondAsync("Adding Key...");
        string apiKey = command.Data.Options.FirstOrDefault()?.Value.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(apiKey))
        {
            await command.Channel.SendMessageAsync("Api key could not be parsed. Please try again.");
            await command.DeleteOriginalResponseAsync();
            return;
        }

        var serverConnectionDataCollection = _serviceProvider.GetRequiredService<ServerConnectionDataCollection>();
        var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

        var serverConnectionData = serverConnectionDataCollection.GetServerConnection(command.GuildId);
        if (serverConnectionData == null)
        {
            var guild = client.GetGuild(command.GuildId!.Value);
            if (guild == null)
            {
                await command.Channel.SendMessageAsync("Server could not be found.");
                await command.DeleteOriginalResponseAsync();
                return;
            }

            serverConnectionDataCollection.AddNewServerConnection(guild, apiKey);
        }
        else
        {
            await serverConnectionData.AddApiKey(apiKey);
        }

        _ = Task.FromResult(serverConnectionDataCollection.AddAPIKeyToBackupFile(command.GuildId!.Value, apiKey));
        await command.Channel.SendMessageAsync("API key added successfully! Check / ai for new options.");
        await command.DeleteOriginalResponseAsync();
    }

    private async Task HandleRemoveKeyCommand(SocketSlashCommand command)
    {
        await command.RespondAsync("Removing key...");
        string apiKey = command.Data.Options.FirstOrDefault()?.Value.ToString()?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(apiKey))
        {
            await command.Channel.SendMessageAsync("Api key could not be parsed. Please try again.");
            await command.DeleteOriginalResponseAsync();
            return;
        }

        var serverConnectionDataCollection = _serviceProvider.GetRequiredService<ServerConnectionDataCollection>();

        var serverConnectionData = serverConnectionDataCollection.GetServerConnection(command.GuildId);
        if (serverConnectionData != null)
        {
            await serverConnectionData.RemoveApiKey(apiKey);
            _ = Task.FromResult(serverConnectionDataCollection.RemoveAPIKeyFromBackupFile(command.GuildId!.Value, apiKey));
            await command.Channel.SendMessageAsync("API key removed successfully! /ai has been updated");
            await command.DeleteOriginalResponseAsync();
        }
        else
        {
            await command.Channel.SendMessageAsync("No API key has been given previously to remove.");
            await command.DeleteOriginalResponseAsync();
        }
    }

    private async Task HandleClearKeysCommand(SocketSlashCommand command)
    {
        var serverConnectionDataCollection = _serviceProvider.GetRequiredService<ServerConnectionDataCollection>();
        
        if (await serverConnectionDataCollection.RemoveServerConnection(command.GuildId))
        {
            _ = Task.FromResult(serverConnectionDataCollection.ClearAllAPIKeysFromFile(command.GuildId!.Value));
            await command.RespondAsync("All API keys have been wiped.");
        }
        else
        {
            await command.RespondAsync("No API keys have been provided to wipe.");
        }
    }

    private async Task HandleRefreshCommand(SocketSlashCommand command)
    {
        var serverConnectionData = _serviceProvider.GetRequiredService<ServerConnectionDataCollection>()
                                                    .GetServerConnection(command.GuildId);

        if (serverConnectionData != null)
        {
            var _ = Task.FromResult(serverConnectionData.RefreshVoicesAndCommands());
            await command.RespondAsync($"Refreshing Voices...");
        }
        else
        {
            await command.RespondAsync($"No API Key has been provided. Please contact an admin to provide key.");
        }
    }

    private async Task HandleJoinCommand(SocketSlashCommand command)
    {
        var vc = (command.User as IGuildUser)?.VoiceChannel;
        var vcService = _serviceProvider.GetRequiredService<ServerConnectionDataCollection>()
                                        .GetServerConnection(command.GuildId);
        if (vcService == null)
        {
            await command.RespondAsync($"Could not join channel. If an ElevenLabs api key has not been provided, please get an admin to provide one.");
        }
        else if (vc == null)
        {
            await command.RespondAsync($"User not in Voice Channel to join.");
        }
        else
        {
            _ = Task.FromResult(vcService.Connect(vc));
            await command.RespondAsync($"Joining {vc.Name}");
        }
    }

    private async Task HandleLeaveCommand(SocketSlashCommand command)
    {
        var vcService = _serviceProvider.GetRequiredService<ServerConnectionDataCollection>()
                                        .GetServerConnection(command.GuildId);
        if (vcService != null)
        {
            _ = Task.FromResult(vcService.Disconnect());
                await command.RespondAsync($"Leaving VC");
        }
        else
        {
            await command.RespondAsync($"Server instance not found");
        }
    }

    private async Task HandleTTSCommand(SocketSlashCommand command)
    {
        string voiceId = command.Data.Options.FirstOrDefault()?.Value.ToString()?.Trim() ?? string.Empty;
        string text = command.Data.Options.ElementAt(1)?.Value.ToString()?.Trim() ?? string.Empty;

        double stability = 0.75;
        if (command.Data.Options.Any(o => o.Name == "stability"))
        {
            int stabilityInt = Convert.ToInt32(command.Data.Options.First(o => o.Name == "stability").Value);
            if (stabilityInt >= 0 && stabilityInt <= 100)
                stability = stabilityInt / 100.0;
        }

        if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(voiceId))
        {
            if (text.Count() > 750)
            {
                await command.RespondAsync($"Generated text is too long... Max character limit is set to 750 currently...");
                return;
            }

            var vcService = _serviceProvider.GetRequiredService<ServerConnectionDataCollection>()
                                            .GetServerConnection(command.GuildId);

            if (vcService == null)
            {
                await command.RespondAsync($"Server instance not found.");
                return;
            }
            else
            {
                var _ = Task.FromResult(vcService.GenerateVoiceLine(voiceId, text, command.GuildId, stability, command));
                await command.RespondAsync("Generating...");
            }
        }
        else
        {
            await command.RespondAsync($"One or more options were not provided correctly.");
        }
    }

    private async Task Client_Ready()
    {
        var client = _serviceProvider.GetRequiredService<DiscordSocketClient>();

        var addKeyCommand = new SlashCommandBuilder()
            .WithName("addkey")
            .WithDMPermission(false)
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
            .WithDescription("Adds provided ElevenLabs key to server and allows for voice use")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("key")
                .WithDescription("The Eleven Labs key associated with your account")
                .WithType(ApplicationCommandOptionType.String)
                .WithRequired(true)
            );

        var removeKeyCommand = new SlashCommandBuilder()
            .WithName("removekey")
            .WithDMPermission(false)
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
            .WithDescription("Removes provided ElevenLabs key from server and removes associated voice options")
            .AddOption(new SlashCommandOptionBuilder()
                .WithName("key")
                .WithDescription("The Eleven Labs key associated with your account")
                .WithType(ApplicationCommandOptionType.String)
                .WithRequired(true)
            );

        var clearKeysCommand = new SlashCommandBuilder()
            .WithName("clearkeys")
            .WithDMPermission(false)
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
            .WithDescription("Removes ALL ElevenLabs keys from server. Disables /ai usage until new key provided");

        var refreshCommand = new SlashCommandBuilder()
            .WithName("refresh")
            .WithDMPermission(false)
            .WithDefaultMemberPermissions(GuildPermission.Administrator)
            .WithDescription("Refreshes Available voices from provided ElevenLabs keys");

        var joinCommand = new SlashCommandBuilder()
            .WithName("join")
            .WithDMPermission(false)
            .WithDescription("Join the user's Voice Channel");

        var leaveCommand = new SlashCommandBuilder()
            .WithName("leave")
            .WithDMPermission(false)
            .WithDescription("Disconnects from Voice Channel");

        try
        {
            await client.CreateGlobalApplicationCommandAsync(joinCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(leaveCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(refreshCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(addKeyCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(removeKeyCommand.Build());
            await client.CreateGlobalApplicationCommandAsync(clearKeysCommand.Build());
        }
        catch(HttpException ex)
        {
            var json = JsonSerializer.Serialize(ex.Errors);
            Console.WriteLine(json);
        }

        HandleCreateProgramFileSystem();
    }

    private void HandleCreateProgramFileSystem()
    {
        DirectoryInfo parentDi = new DirectoryInfo(BotData.VoiceFileLoc);
        if (parentDi.Exists)
            foreach (var di in parentDi.GetDirectories())
            {
                foreach (var file in di.GetFiles())
                {
                    file.Delete();
                }
                di.Delete();
            }
        else
            Directory.CreateDirectory(BotData.VoiceFileLoc);

        DirectoryInfo backupDi = new DirectoryInfo(BotData.APIKeyBackupFileLoc);
        if (!backupDi.Exists)
            backupDi.Create();

        if (!File.Exists(BotData.APIKeyBackupFile))
        {
            string json = JsonSerializer.Serialize(new Dictionary<ulong, IEnumerable<string>>());
            _ = Task.FromResult(File.WriteAllTextAsync(BotData.APIKeyBackupFile, json));
        }
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    private static IServiceProvider CreateProvider()
    {
        var config = new DiscordSocketConfig()
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
        };

        var collection = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton<DiscordSocketClient>()
            .AddSingleton<TTSService>()
            .AddSingleton<ServerConnectionDataCollection>();
        
        return collection.BuildServiceProvider();
    }

    private static void BuildDataModels()
    {
        var builder = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", false, true);
        
        var config = builder.Build();
        BotData.Token = config["Bot:token"] ?? throw new NotImplementedException("No Token found for bot...");
        if (string.IsNullOrWhiteSpace(BotData.Token))
            throw new NotImplementedException("Please provide your Discord Bot Token in the appsettings.json file");
    }
}
