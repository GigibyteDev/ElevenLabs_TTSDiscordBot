using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevenLabsTTSDiscordBot.DataModels
{
    public class VoiceHistory
    {
        public List<VoiceHistoryData> history { get; set; }
    }

    public class VoiceHistoryData
    {
        public string history_item_id { get; set; }
        public string voice_id { get; set; }
        public string voice_name { get; set; }
        public string text { get; set; }
        public VoiceHistoryDataSettings settings { get; set; }
    }

    public class VoiceHistoryDataSettings
    {
        public double similarity_boost { get; set; }
        public double stability { get; set; }
    }
}
