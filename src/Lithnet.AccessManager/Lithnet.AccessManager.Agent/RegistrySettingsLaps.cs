using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsLaps : RegistrySettings, ILapsSettings
    {
        private const string keyName = "Lithnet\\Access Manager Agent\\Password";

        public RegistrySettingsLaps() : base(keyName, true)
        {
        }

        internal RegistrySettingsLaps(string key) : base(key, false)
        {
        }

        public bool Enabled => this.GetValue<int>("Enabled", 0) == 1;

        public int PasswordLength => this.GetValue<int>("PasswordLength", 16);

        public string PasswordCharacters => this.GetValue<string>("PasswordCharacters", null);

        public bool UseUpper => this.GetValue<int>("UseUpper", 0) == 1;

        public bool UseLower => this.GetValue<int>("UseLower", 0) == 1;

        public bool UseSymbol => this.GetValue<int>("UseSymbol", 0) == 1;

        public bool UseNumeric => this.GetValue<int>("UseNumeric", 0) == 1;

        public bool UseReadabilitySeparator => this.GetValue<int>("UseReadabilitySeparator", 0) == 1;

        public string ReadabilitySeparator => this.GetValue<string>("ReadabilitySeparator", "-");

        public int ReadabilitySeparatorInterval => this.GetValue<int>("ReadabilitySeparatorInterval", 4);

        public int PasswordHistoryDaysToKeep => this.GetValue<int>("PasswordHistoryDaysToKeep", 0);
        
        public int MaximumPasswordAge => this.GetValue<int>("MaximumPasswordAge", 14);

        public MsMcsAdmPwdBehaviour MsMcsAdmPwdBehaviour => (MsMcsAdmPwdBehaviour)(this.GetValue<int>("MsMcsAdmPwdBehaviour", 0));
    }
}
