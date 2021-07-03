using Lithnet.AccessManager.Api.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Lithnet.AccessManager.Agent
{
    internal static class Extensions
    {
        public static T GetValue<T>(this RegistryKey key, string name)
        {
            return key.GetValue<T>(name, default(T));
        }

        public static T GetValue<T>(this RegistryKey key, string name, T defaultValue)
        {
            object rawvalue = key?.GetValue(name);

            if (rawvalue == null)
            {
                return defaultValue;
            }

            try
            {
                return (T)Convert.ChangeType(rawvalue, typeof(T));
            }
            catch
            {
            }

            return defaultValue;
        }

        public static IEnumerable<string> GetValues(this RegistryKey key, string name)
        {
            string[] rawvalue = key?.GetValue(name) as string[];

            if (rawvalue == null)
            {
                return new string[] { };
            }

            return rawvalue;
        }

        public static void ConfigureWritable<T>(this IServiceCollection services, IConfigurationSection section, string file = "appsettings.json") where T : class, new()
        {
            services.Configure<T>(section);
            services.AddTransient<IWritableOptions<T>>(provider =>
            {
                IHostEnvironment environment = provider.GetService<IHostEnvironment>();
                IOptionsMonitor<T> options = provider.GetService<IOptionsMonitor<T>>();
                return new WritableOptions<T>(environment, options, section.Key, file);
            });
        }

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