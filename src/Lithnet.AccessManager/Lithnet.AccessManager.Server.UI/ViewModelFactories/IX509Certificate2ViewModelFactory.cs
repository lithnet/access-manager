using System.Security.Cryptography.X509Certificates;

namespace Lithnet.AccessManager.Server.UI
{
    public interface IX509Certificate2ViewModelFactory
    {
        X509Certificate2ViewModel CreateViewModel(X509Certificate2 model);
    }
}