using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using Lithnet.Laps.Web.ActiveDirectory;

namespace Lithnet.Laps.Web.Models
{
    public class LapEntryModel
    {
        [Required]
        public string ComputerName { get; private set; }

        public string Password { get; private set; }

        public string HtmlPassword { get; private set; }

        public DateTime? ValidUntil { get; private set; }

        private LapEntryModel(string computerName, string password, string htmlPassword, DateTime? validUntil)
        {
            this.ComputerName = computerName;
            this.Password = password;
            this.HtmlPassword = htmlPassword;
            this.ValidUntil = validUntil;
        }

        public LapEntryModel(IComputer computer, PasswordData passwordData) : this(computer.MsDsPrincipalName, passwordData.Value,
            LapEntryModel.BuildHtmlPassword(passwordData.Value), passwordData.ExpirationTime)
        {
        }

        public LapEntryModel(IComputer computer, IList<PasswordEntry> passwordData)
        {
            PasswordEntry first = passwordData.First();

            this.ComputerName = computer.MsDsPrincipalName;
            this.Password = first.Password;
            this.HtmlPassword = BuildHtmlPassword(this.Password);
            this.ValidUntil = first.ExpiryDate;
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
                else if (char.IsWhiteSpace(s))
                {
                    builder.Append(@"<span class=""password-char-other"">&nbsp;</span>");
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