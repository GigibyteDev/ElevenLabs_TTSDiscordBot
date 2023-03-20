using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevenLabsTTSDiscordBot.DataModels
{
    public class VoiceDataObj
    {
        public string VoiceName { get; private set; }
        public string APIVoiceId { get; private set; }

        public VoiceDataObj(string voiceName, string apiVoiceId)
        {
            VoiceName = voiceName;
            APIVoiceId = apiVoiceId;
        }
    }
}
