using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Web.Internal
{
    public class NatoPhoneticStringProvider : IPhoneticPasswordTextProvider
    {
        private static Dictionary<char, string> dictionary = new Dictionary<char, string>() {
            { 'a', "alpha" },
            { 'b', "bravo" },
            { 'c', "charlie" },
            { 'd', "delta" },
            { 'e', "echo" },
            { 'f', "foxtrot" },
            { 'g', "golf" },
            { 'h', "hotel" },
            { 'i', "india" },
            { 'j', "juliet" },
            { 'k', "kilo" },
            { 'l', "lima" },
            { 'm', "mike" },
            { 'n', "november" },
            { 'o', "oscar" },
            { 'p', "papa" },
            { 'q', "quebec" },
            { 'r', "romeo" },
            { 's', "sierra" },
            { 't', "tango" },
            { 'u', "uniform" },
            { 'v', "victor" },
            { 'w', "whiskey" },
            { 'x', "x-ray" },
            { 'y', "yankee" },
            { 'z', "zulu" },
            { '0', "zero" } ,
            { '1', "one" } ,
            { '2', "two" } ,
            { '3', "three" } ,
            { '4', "four" } ,
            { '5', "five" } ,
            { '6', "six" } ,
            { '7', "seven" } ,
            { '8', "eight" } ,
            { '9', "nine" },
            { ' ', "space" },
            { '!', "exclamation mark" },
            { '@', "at" },
            { '#', "hash" },
            { '$', "dollar" },
            { '%', "percent" },
            { '^', "caret" },
            { '&', "ampersand" },
            { '*', "asterisk" },
            { '(', "open parenthesis" },
            { ')', "close parenthesis" },
            { '{', "open brace" },
            { '}', "close brace" },
            { '[', "open square bracket" },
            { ']', "close square bracket" },
            { '_', "underscore" },
            { '-', "minus" },
            { '+', "plus" },
            { '=', "equals" },
            { '\\', "back slash" },
            { '/', "forward slash" },
            { ';', "semi colon" },
            { ':', "colon" },
            { '\'', "apostrophe" },
            { '\"', "quote" },
            { '<', "less than" },
            { '>', "greater than" },
            { '.', "period" },
            { ',', "comma" },
            { '|', "pipe" },
            { '~', "tilde" },
            { '`', "back tick" },
            { '£', "pound" },
        };

        public IEnumerable<string> GetPhoneticText(string password)
        {
            int count = 0;

            StringBuilder group = new StringBuilder();

            for (int i = 0; i < password.Length; i++)
            {
                char c = password[i];

                if (count > 3)
                {
                    count = 0;
                    yield return group.ToString();
                    group.Clear();
                }
                else
                {
                    if (i > 0 && i < password.Length)
                    {
                        group.Append(", ");
                    }
                }

                string prefix = string.Empty;

                if (char.IsUpper(c))
                {
                    prefix = "capital ";

                }

                group.Append($"{prefix}{this.GetPhoneticName(char.ToLower(c))}");
                count++;
            }

            if (count > 0)
            {
                yield return group.ToString();
            }
        }

        private string GetPhoneticName(char c)
        {
            if (dictionary.ContainsKey(c))
            {
                return dictionary[c];
            }

            return c.ToString();
        }
    }
}
