using System;
using System.Linq.Expressions;
using System.Security.Cryptography.X509Certificates;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class X509Certificate2ViewModelFactory : IX509Certificate2ViewModelFactory
    {
        public X509Certificate2ViewModelFactory()
        {
        }

        public X509Certificate2ViewModel CreateViewModel(X509Certificate2 model)
        {
            return new X509Certificate2ViewModel(model);
        }
    }
}
