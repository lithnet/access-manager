using System;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Lithnet.Laps.Web.Models
{
    public class LapEntryModel
    {
        [Required]
        public string ComputerName { get; private set; }

        public string Password { get; private set; }

        public string HtmlPassword { get; private set; }
        
        public DateTime? ValidUntil { get; private set; }

        public string FailureReason { get; private set; }

        private LapEntryModel(string computerName, string password, string htmlPassword, DateTime? validUntil,
            string failureReason)
        {
            ComputerName = computerName;
            Password = password;
            HtmlPassword = htmlPassword;
            ValidUntil = validUntil;
        }

        public LapEntryModel(IComputer computer, Password password) : this(computer.SamAccountName, password.Value,
            BuildHtmlPassword(password.Value), password.ExpirationTime, String.Empty)
        {
        }

        private static string BuildHtmlPassword(string password)
        {
            StringBuilder builder = new StringBuilder();

            foreach (char s in password)
            {
                if (char.IsDigit(s))
                {
                    builder.AppendFormat(@"<span class=""password-char-digit"">{0}</span>", s);
                }
                else if (char.IsLetter(s))
                {
                    builder.AppendFormat(@"<span class=""password-char-letter"">{0}</span>", s);
                }
                else
                {
                    builder.AppendFormat(@"<span class=""password-char-other"">{0}</span>", s);
                }
            }

            return builder.ToString();
        }
    }
}