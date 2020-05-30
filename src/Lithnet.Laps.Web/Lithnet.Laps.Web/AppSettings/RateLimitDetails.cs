using System;
using Microsoft.Extensions.Configuration;

namespace Lithnet.Laps.Web
{
    public class RateLimitDetails : IRateLimitDetails
    {
        private IConfigurationSection configuration;

        public RateLimitDetails(IConfigurationSection configuration)
        {
            this.configuration = configuration;
        }

        public bool Enabled
        {
            get
            {
                string value = this.configuration["enabled"];

                if (bool.TryParse(value, out bool result))
                {
                    return result;
                }

                return false;
            }
        }

        public int ReqPerMinute
        {
            get
            {
                string value = this.configuration["requestsPerMinute"];

                if (int.TryParse(value, out int result))
                {
                    if (result <= 0)
                    {
                        return 10;
                    }

                    return result;
                }

                return 10;
            }
        }

        public int ReqPerHour
        {
            get
            {
                string value = this.configuration["requestsPerHour"];

                if (int.TryParse(value, out int result))
                {
                    if (result <= 0)
                    {
                        return 50;
                    }

                    return result;
                }

                return 50;
            }
        }

        public int ReqPerDay
        {
            get
            {
                string value = this.configuration["requestsPerDay"];

                if (int.TryParse(value, out int result))
                {
                    if (result <= 0)
                    {
                        return 100;
                    }

                    return result;
                }

                return 100;
            }
        }
    }
}