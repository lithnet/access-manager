using System;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    internal static class Extensions
    {
        public static string GetEnumDescription(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

            return attribute != null ? attribute.Description : value.ToString();
        }

        public static string ToDisplayName(this X509Certificate2 cert)
        {
            if (cert == null)
            {
                return null;
            }

            StringBuilder builder = new StringBuilder();

            string dnsname = cert.GetNameInfo(X509NameType.DnsName, false);

            if (!string.IsNullOrWhiteSpace(dnsname))
            {
                builder.Append("[");
                builder.Append(dnsname);
                builder.Append("], ");
            }

            if (!string.IsNullOrWhiteSpace(cert.Subject))
            {
                builder.Append("[");
                builder.Append(cert.Subject);
                builder.Append("], ");
            }
         
            if (!string.IsNullOrWhiteSpace(cert.FriendlyName))
            {
                builder.Append("[");
                builder.Append(cert.FriendlyName);
                builder.Append("], ");
            }

            if (!string.IsNullOrWhiteSpace(cert.Issuer))
            {
                if (cert.Issuer == cert.Subject)
                {
                    builder.Append("[self-signed], ");
                }
                else
                {
                    string issuer = cert.GetNameInfo(X509NameType.SimpleName, true);
                    builder.Append("[Issuer: ");
                    builder.Append(issuer);
                    builder.Append("], ");
                }
            }

            builder.Append("[Issued: ");
            builder.Append(cert.NotBefore.ToShortDateString());
            builder.Append("]");

            return builder.ToString();
        }

        public static IntPtr GetHandle(this IViewAware view)
        {
            Window window = Window.GetWindow(view.View);
            var wih = new WindowInteropHelper(window);
            return wih.Handle;
        }

        public static Window GetWindow(this IViewAware view)
        {
            return Window.GetWindow(view.View);
        }
    }
}
