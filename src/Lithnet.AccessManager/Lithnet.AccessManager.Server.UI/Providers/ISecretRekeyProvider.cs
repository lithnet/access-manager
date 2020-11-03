using System.Threading.Tasks;
using MahApps.Metro.Controls.Dialogs;

namespace Lithnet.AccessManager.Server.UI.Providers
{
    public interface ISecretRekeyProvider
    {
        Task<bool> TryReKeySecretsAsync(object context);
    }
}