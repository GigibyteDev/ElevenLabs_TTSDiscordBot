using Discord;

namespace ElevenLabsTTSDiscordBot
{
    public class ServerConnectionDataCollection
    {
        private Dictionary<ulong, ServerConnectionData> _serverConnections { get; set; } = new Dictionary<ulong, ServerConnectionData>();
        private TTSService _service { get; set; }
        public ServerConnectionDataCollection(TTSService service)
        {
            _service = service;
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
