using System;
using System.Collections.Generic;
using System.Net;
using System.Web;

namespace Lithnet.Laps.Web.Internal
{
    public static class TokenReplacer
    {
        public static string ReplaceAsPlainText(Dictionary<string, string> tokens, string text)
        {
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text?.Replace(token.Key, token.Value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }

        public static string ReplaceAsHtml(Dictionary<string, string> tokens, string text)
        {
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text?.Replace(token.Key, WebUtility.HtmlEncode(token.Value ?? string.Empty), StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }

        public static string ReplaceAsJson(Dictionary<string, string> tokens, string text)
        {
            foreach (KeyValuePair<string, string> token in tokens)
            {
                text = text?.Replace(token.Key, HttpUtility.JavaScriptStringEncode(token.Value ?? string.Empty), StringComparison.OrdinalIgnoreCase);
            }

            return text;
        }
    }
}
