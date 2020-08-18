using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server.Extensions
{
    public static class Extensions
    {
        public static string GetSecret(this EncryptedData data)
        {
            try
            {
                if (data?.Data == null)
                {
                    return null;
                }

                if (data.Mode == 0)
                {
                    return data.Data;
                }

                if (data.Mode != 1)
                {
                    throw new ConfigurationException("The data was protected with an encryption mechanism not known to this version of the application");
                }

                byte[] salt = Convert.FromBase64String(data.Salt);
                byte[] protectedData = Convert.FromBase64String(data.Data);
                byte[] unprotectedData = ProtectedData.Unprotect(protectedData, salt, DataProtectionScope.LocalMachine);

                return Encoding.UTF8.GetString(unprotectedData);
            }
            catch (Exception ex)
            {
                throw new ConfigurationException("Unable to decrypt the encrypted data from the configuration file. Use the configuration manager application to re-enter the secret, and try again", ex);
            }
        }

        public static void ValidateAccessMask(this AccessMask requestedAccess)
        {
            if (requestedAccess == 0)
            {
                throw new AccessManagerException($"An invalid access mask combination was requested: {requestedAccess}");
            }

            if (requestedAccess == AccessMask.Jit ||
                requestedAccess == AccessMask.LocalAdminPassword ||
                requestedAccess == AccessMask.LocalAdminPasswordHistory ||
                requestedAccess == AccessMask.BitLocker)
            {
                return;
            }

            throw new AccessManagerException($"An invalid access mask combination was requested: {requestedAccess}");
        }

        public static void ForEach<T>(this IEnumerable<T> e, Action<T> action)
        {
            foreach (T item in e)
            {
                action(item);
            }
        }

        public static string ToDescription(this Enum value)
        {
            FieldInfo fi = value.GetType().GetField(value.ToString());

            if (fi?.GetCustomAttributes(typeof(DescriptionAttribute), false) is DescriptionAttribute[] attributes && attributes.Any())
            {
                return attributes.First().Description;
            }

            return value.ToString();
        }
    }
}
