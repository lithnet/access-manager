using Microsoft.Win32;

namespace Lithnet.AccessManager.Agent
{
    public class RegistrySettingsLaps : RegistrySettings, ILapsSettings
    {
        private const string keyName = "Lithnet\\Access Manager Agent\\Password";

        public RegistrySettingsLaps() : base(keyName)
        {
        }

        public bool Enabled => this.GetKey().GetValue<int>("Enabled", 0) == 1;

        public int PasswordLength => this.GetKey().GetValue<int>("PasswordLength", 16);

        public string PasswordCharacters => this.GetKey().GetValue<string>("PasswordCharacters", null);

        public bool UseUpper => this.GetKey().GetValue<int>("UseUpper", 0) == 1;

        public bool UseLower => this.GetKey().GetValue<int>("UseLower", 0) == 1;

        public bool UseSymbol => this.GetKey().GetValue<int>("UseSymbol", 0) == 1;

        public bool UseNumeric => this.GetKey().GetValue<int>("UseNumeric", 0) == 1;

        public bool UseReadabilitySeparator => this.GetKey().GetValue<int>("UseReadabilitySeparator", 0) == 1;

        public string ReadabilitySeparator => this.GetKey().GetValue<string>("ReadabilitySeparator", "-");

        public int ReadabilitySeparatorInterval => this.GetKey().GetValue<int>("ReadabilitySeparatorInterval", 4);

        public int PasswordHistoryDaysToKeep => this.GetKey().GetValue<int>("PasswordHistoryDaysToKeep", 0);
        
        public int MaximumPasswordAge => this.GetKey().GetValue<int>("MaximumPasswordAge", 14);

        public MsMcsAdmPwdBehaviour MsMcsAdmPwdBehaviour => (MsMcsAdmPwdBehaviour)(this.GetKey().GetValue<int>("MsMcsAdmPwdBehaviour", 0));
    }
}
