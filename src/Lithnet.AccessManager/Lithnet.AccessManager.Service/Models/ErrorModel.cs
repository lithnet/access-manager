using System.ComponentModel;
using Lithnet.AccessManager.Service.App_LocalResources;

namespace Lithnet.AccessManager.Service.Models
{
    [Localizable(true)]
    public class ErrorModel
    {
        public ErrorModel()
        {
            this.Message = UIMessages.UnexpectedError;
            this.Heading = UIMessages.AuthNError;
        }

        public string Message { get; set; }

        public string Heading { get; set; } 
    }
}