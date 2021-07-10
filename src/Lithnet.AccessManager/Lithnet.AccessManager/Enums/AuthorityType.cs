using System.ComponentModel;

namespace Lithnet.AccessManager
{
    public enum AuthorityType
    {
        [Description("None")]
        None = 0,

        [Description("Active Directory")]
        ActiveDirectory = 1,

        [Description("Azure Active Directory")]
        AzureActiveDirectory = 2,

        [Description("Access Manager")]
        Ams = 3
    }
}