using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Graph;

namespace Lithnet.AccessManager.Api
{
    public static class GraphExtensions
    {
        public static IEnumerable<string> GetDeviceCertificateThumbprints(this Microsoft.Graph.Device device)
        {
            if (device.AlternativeSecurityIds == null)
            {
                yield break;
            }

            foreach (AlternativeSecurityId securityId in device.AlternativeSecurityIds)
            {
                if (securityId.Type != 2 || securityId.Key == null)
                {
                    continue;
                }

                string data = System.Text.Encoding.Unicode.GetString(securityId.Key);

                //X509:<SHA1-TP-PUBKEY>1BBF43F156D6544D87BD98580273C75F0344DA56zw4/fMMN7MbkH3DB7XrbDgnGchQ/wkRHkkb32d2M5Nk=
                //                     ^                                      ^

                if (data.Length < 80)
                {
                    continue;
                }

                yield return data.Substring(21, 40);
            }
        }

        public static bool HasDeviceThumbprint(this Microsoft.Graph.Device device, string thumbprint)
        {
            foreach (string tp in device.GetDeviceCertificateThumbprints())
            {
                if (string.Equals(tp, thumbprint, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static void ThrowOnDeviceDisabled(this Microsoft.Graph.Device device)
        {
            if (device.AccountEnabled == null || device.AccountEnabled.Value == false)
            {
                throw new DeviceDisabledException($"The AAD device {device.DeviceId} is disabled");
            }
        }
    }
}
