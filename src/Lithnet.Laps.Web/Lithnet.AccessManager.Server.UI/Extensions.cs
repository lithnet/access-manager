using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Lithnet.AccessManager.Server.UI.Interop;
using Stylet;
using TimeoutException = System.TimeoutException;

namespace Lithnet.AccessManager.Server.UI
{
    internal static class Extensions
    {
        public static void AddPrivateKeyReadPermission(this X509Certificate2 cert, IdentityReference account)
        {
            string location = NativeMethods.GetKeyLocation(cert);

            if (location == null)
            {
                throw new CertificateNotFoundException("The certificate private key was not found. Manually add permissions for the service account to read this private key");
            }

            FileInfo info = new FileInfo(location);
            info.AddFileSecurity(account, FileSystemRights.Read, AccessControlType.Allow);
        }

        public static void AddFileSecurity(this FileInfo info, IdentityReference account, FileSystemRights rights, AccessControlType controlType)
        {
            FileSecurity fSecurity = info.GetAccessControl();

            fSecurity.AddAccessRule(new FileSystemAccessRule(account, rights, controlType));

            info.SetAccessControl(fSecurity);
        }

        public static async Task WaitForStatusAsync(this ServiceController controller, ServiceControllerStatus desiredStatus, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            controller.Refresh();
            while (controller.Status != desiredStatus)
            {
                if (DateTime.UtcNow - utcNow > timeout)
                {
                    throw new TimeoutException($"Failed to wait for '{controller.ServiceName}' to change status to '{desiredStatus}'.");
                }

                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                controller.Refresh();
            }
        }

        public static async Task WaitForChangeAsync(this ServiceController controller, CancellationToken cancellationToken)
        {
            controller.Refresh();
            var status = controller.Status;

            while (controller.Status == status)
            {
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                controller.Refresh();
            }
        }

        public static string GetEnumDescription(this Enum value)
        {
            var fieldInfo = value.GetType().GetField(value.ToString());
            var attribute = fieldInfo?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;

            return attribute?.Description ?? value.ToString();
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
