using System.ComponentModel;
using Lithnet.AccessManager.Service.App_LocalResources;

namespace Lithnet.AccessManager.Service.Models
{
    [Localizable(true)]
    public class NoPasswordModel
    {
        public NoPasswordModel()
        {
        }

        public string ComputerName { get; set; }

        public string Message { get; set; }

        public string Heading { get; set; } 
    }
}