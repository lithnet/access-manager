using System.Collections.Generic;

namespace Lithnet.AccessManager.Server.Configuration
{
    public class PhoneticSettings
    {
        public Dictionary<string, string> CharacterMappings { get; set; } = new Dictionary<string, string>();

        public string PhoneticNameColon { get; set; }

        public string UpperPrefix { get; set; } = "capital";

        public string LowerPrefix { get; set; }

        public int GroupSize { get; set; } = 4;

        public bool HidePhoneticBreakdown { get; set; }

        public bool DisableTextToSpeech { get; set; }
    }
}