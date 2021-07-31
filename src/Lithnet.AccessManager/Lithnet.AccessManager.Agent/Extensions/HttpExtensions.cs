using Lithnet.AccessManager.Api.Shared;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Lithnet.AccessManager.Agent
{
    internal static class HttpExtensions
    {
        public static void EnsureSuccessStatusCode(this HttpResponseMessage message, string content)
        {
            if (message.IsSuccessStatusCode)
            {
                return;
            }

            throw message.CreateException(content);
        }

        public static Exception CreateException(this HttpResponseMessage message, string content)
        {
            try
            {
                JsonDocument j = JsonDocument.Parse(content);
                if (j.RootElement.TryGetProperty("Error", out var details))
                {
                    ApiError error = JsonSerializer.Deserialize<ApiError>(details.GetRawText());
                    return new ApiException(error, message);
                }
            }
            catch
            {
                // ignore
            }

            string messageContent = string.IsNullOrWhiteSpace(content) ? string.Empty : $"Content: {content}";

            return new HttpRequestException(string.Format(
                System.Globalization.CultureInfo.InvariantCulture,
                "Response status code does not indicate success: {0} ({1}).\r\n{2}",
                (int)message.StatusCode,
                message.ReasonPhrase,
                messageContent)
            );
        }

        public static StringContent AsJsonStringContent(this object o) => new StringContent(JsonSerializer.Serialize(o), Encoding.UTF8, "application/json");
    }
}