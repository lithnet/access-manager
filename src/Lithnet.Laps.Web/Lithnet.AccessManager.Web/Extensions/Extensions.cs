using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Web.Authorization;

namespace Lithnet.AccessManager.Web.Internal
{
    internal static class Extensions
    {
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

        internal static void ValidateAccessMask(this AccessMask requestedAccess)
        {
            if (requestedAccess == 0)
            {
                throw new AccessManagerException($"An invalid access mask combination was requested: {requestedAccess}");
            }

            if (requestedAccess == AccessMask.Jit ||
                requestedAccess == AccessMask.Laps ||
                requestedAccess == AccessMask.LapsHistory)
            {
                return;
            }

            throw new AccessManagerException($"An invalid access mask combination was requested: {requestedAccess}");
        }
    }
}