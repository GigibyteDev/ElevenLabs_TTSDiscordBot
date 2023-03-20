namespace ElevenLabsTTSDiscordBot.DataModels
{
    public class Voices
    {
        public List<VoiceObj> voices { get; set; }
    }
    public class VoiceObj
    {
        public string voice_id { get; set; }
        public string name { get; set; }
        public string category { get; set; }
    }
}
