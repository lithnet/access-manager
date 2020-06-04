using System;
using System.Collections.Generic;

namespace Lithnet.Laps.Web.Internal
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
    }
}