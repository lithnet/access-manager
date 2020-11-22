using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Lithnet.AccessManager.Server.Configuration;

namespace Lithnet.AccessManager.Server
{
    public static class Extensions
    {
        public static string ToCommaSeparatedString(this X500DistinguishedName dn)
        {
            string decoded = dn?.Decode(X500DistinguishedNameFlags.UseNewLines);

            if (decoded == null)
            {
                return null;
            }

            return string.Join(',', decoded.Split("\r\n"));
        }

        public static string ToHexString(this byte[] hash)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash), "The binary value provided was null");
            }

            return hash.ToHexString(0, hash.Length);
        }

        public static string ToHexString(this byte[] hash, int offset, int count)
        {
            if (hash == null)
            {
                throw new ArgumentNullException(nameof(hash), "The binary value provided was null");
            }

            if (offset >= hash.Length)
            {
                throw new ArgumentException("The value for offset cannot exceed the length of the hash", nameof(offset));
            }

            if (count + offset > hash.Length)
            {
                throw new ArgumentException("The combined values of offset and count cannot exceed the length of the hash", nameof(offset));
            }

            StringBuilder sb = new StringBuilder(hash.Length * 2);

            for (int i = offset; i < count; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }

            return sb.ToString();
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
