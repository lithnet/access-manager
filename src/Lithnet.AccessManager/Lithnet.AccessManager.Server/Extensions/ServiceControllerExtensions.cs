using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Server
{
    internal static class ServiceControllerExtensions
    {
        public static async Task WaitForStatusAsync(this ServiceController controller, ServiceControllerStatus desiredStatus, TimeSpan timeout, CancellationToken cancellationToken)
        {
            var utcNow = DateTime.UtcNow;
            controller.Refresh();

            while (controller.Status != desiredStatus)
            {
                if (DateTime.UtcNow - utcNow > timeout)
                {
                    throw new System.TimeoutException($"Failed to wait for '{controller.ServiceName}' to change status to '{desiredStatus}'.");
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
    }
}
