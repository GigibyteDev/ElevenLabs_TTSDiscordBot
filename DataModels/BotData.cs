

namespace ElevenLabsTTSDiscordBot.DataModels
{
    public static class BotData
    {
        public static string Token { get; set; }
        public static string VoiceFileLoc { get; set; } = "..\\..\\..\\VoiceFiles\\";

        public static string APIKeyBackupFileLoc { get; set; } = "..\\..\\..\\ServerDataBackup\\";
        public static string APIKeyBackupFile { get; set; } = APIKeyBackupFileLoc + "APIKeys.json";
    }
}
