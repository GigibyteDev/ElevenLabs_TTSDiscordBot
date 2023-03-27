using Discord;
using Discord.WebSocket;
using ElevenLabsTTSDiscordBot.DataModels;
using System.Text.Json;

namespace ElevenLabsTTSDiscordBot
{
    public class ServerConnectionDataCollection
    {
        private Dictionary<ulong, ServerConnectionData> _serverConnections { get; set; } = new Dictionary<ulong, ServerConnectionData>();
        private TTSService _service { get; set; }
        private DiscordSocketClient client { get; set; }
        public ServerConnectionDataCollection(DiscordSocketClient client, TTSService service)
        {
            this.client = client;
            _service = service;
            _ = Task.FromResult(InstantiateFromFile());
        }

        private async Task InstantiateFromFile()
        {
            string json = File.ReadAllText(BotData.APIKeyBackupFile);

            var apiKeys = JsonSerializer.Deserialize<Dictionary<ulong, IEnumerable<string>>>(json);
            if (apiKeys == null) throw new NotImplementedException("API Key Backup could not be loaded...");
            
            foreach (var serverKeys in apiKeys)
            {
                if (serverKeys.Value.Count() > 0)
                {
                    var conn = AddNewServerConnection(client.GetGuild(serverKeys.Key), serverKeys.Value.First());
                    if (serverKeys.Value.Count() > 1)
                    {
                        for (int i = 1; i < serverKeys.Value.Count(); i++)
                        {
                            await conn.AddApiKey(serverKeys.Value.ElementAt(i));
                        }
                    }
                }
            }
        }

        public async Task AddAPIKeyToBackupFile(ulong serverId, string apiKey)
        {
            string json = string.Empty;
            json = await File.ReadAllTextAsync(BotData.APIKeyBackupFile);
            

            var apiKeys = JsonSerializer.Deserialize<Dictionary<ulong, IEnumerable<string>>>(json);
            if (apiKeys == null) throw new NotImplementedException("API Key Backup could not be loaded...");

            IEnumerable<string> keys;
            if (apiKeys.TryGetValue(serverId, out keys))
            {
                if (!keys.Any(k => k == apiKey))
                {
                    apiKeys[serverId].Append(apiKey);
                }
            }
            else
            {
                apiKeys.Add(serverId, new List<string> { apiKey });
            }

            string newJson = JsonSerializer.Serialize(apiKeys);
            await File.WriteAllTextAsync(BotData.APIKeyBackupFile, newJson);
        }

        public async Task ClearAllAPIKeysFromFile(ulong serverId)
        {
            string json = await File.ReadAllTextAsync(BotData.APIKeyBackupFile);

            var apiKeys = JsonSerializer.Deserialize<Dictionary<ulong, IEnumerable<string>>>(json);
            if (apiKeys == null) throw new NotImplementedException("API Key Backup could not be loaded...");

            if (apiKeys.ContainsKey(serverId))
            {
                apiKeys.Remove(serverId);
            }

            string newJson = JsonSerializer.Serialize(apiKeys);
            await File.WriteAllTextAsync(BotData.APIKeyBackupFile, newJson);
        }

        public async Task RemoveAPIKeyFromBackupFile(ulong serverId, string apiKey)
        {
            string json = await File.ReadAllTextAsync(BotData.APIKeyBackupFile);

            var apiKeys = JsonSerializer.Deserialize<Dictionary<ulong, IEnumerable<string>>>(json);
            if (apiKeys == null) throw new NotImplementedException("API Key Backup could not be loaded...");

            IEnumerable<string> keys;
            if (apiKeys.TryGetValue(serverId, out keys))
            {
                if (keys.Any(k => k == apiKey))
                {
                    apiKeys[serverId] = apiKeys[serverId].Where(k => k != apiKey);
                }
            }

            string newJson = JsonSerializer.Serialize(apiKeys);
            await File.WriteAllTextAsync(BotData.APIKeyBackupFile, newJson);
        }

        public ServerConnectionData? GetServerConnection(ulong? id)
        {
            if (!id.HasValue)
                return null;

            ServerConnectionData? data;
            if (_serverConnections.TryGetValue(id.Value, out data))
            {
                return data;
            }

            return null;
        }

        public ServerConnectionData AddNewServerConnection(IGuild server, string apiKey) 
        {
            if (_serverConnections.ContainsKey(server.Id)) return _serverConnections[server.Id];

            _serverConnections.Add(server.Id, new ServerConnectionData(server, apiKey, _service));

            return _serverConnections[server.Id];
        }

        public async Task<bool> RemoveServerConnection(ulong? id)
        {
            if (!id.HasValue)
                return false;

            ServerConnectionData? data;
            if (_serverConnections.TryGetValue(id.Value, out data))
            {
                await data.ClearApiKeys();
                _serverConnections.Remove(id.Value);
                return true;
            }

            return false;
        }
    }
}
