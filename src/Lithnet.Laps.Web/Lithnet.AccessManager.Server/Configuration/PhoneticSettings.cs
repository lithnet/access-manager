using System.Collections.Generic;

namespace Lithnet.AccessManager.Configuration
{
    public class PhoneticSettings
    {
        public Dictionary<string, string> CharacterMappings { get; set; } = new Dictionary<string, string>();

        public string PhoneticNameColon { get; set; }

        public string UpperPrefix { get; set; }

        public string LowerPrefix { get; set; }

        public int GroupSize { get; set; }
    }
}