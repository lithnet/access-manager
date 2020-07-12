using System;
using System.Security.Cryptography.X509Certificates;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class X509Certificate2ViewModel : PropertyChangedBase
    {
        public X509Certificate2ViewModel(X509Certificate2 model)
        {
            this.Model = model;
        }

        public X509Certificate2 Model { get; }

        public string Subject => this.Model?.Subject;

        public DateTime NotBefore => this.Model.NotBefore;

        public DateTime NotAfter => this.Model.NotAfter;

        public bool IsPublished { get; set; }

        public bool IsOrphaned { get; set; }

        public bool HasPrivateKey => this.Model.HasPrivateKey;

        public bool HasNoPrivateKey => !this.HasPrivateKey;
    }
}
