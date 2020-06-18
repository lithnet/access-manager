using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Web.Internal
{
    public class HtmlPasswordProvider : IHtmlPasswordProvider
    {
        public string GetHtmlPassword(string password)
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
