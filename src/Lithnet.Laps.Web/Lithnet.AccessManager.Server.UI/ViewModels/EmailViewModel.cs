using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.IconPacks;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class EmailViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly EmailOptions model;

        public EmailViewModel(EmailOptions model, INotifiableEventPublisher eventPublisher)
        {
            this.model = model;
            eventPublisher.Register(this);
        }

        [NotifiableProperty]
        public string FromAddress { get => this.model.FromAddress; set => this.model.FromAddress = value; }

        [NotifiableProperty]
        public int Port { get => this.model.Port; set => this.model.Port = value; }

        [NotifiableProperty]
        public string Host { get => this.model.Host; set => this.model.Host = value; }

        [NotifiableProperty]
        public bool UseSsl { get => this.model.UseSsl; set => this.model.UseSsl = value; }

        [NotifiableProperty]
        public bool UseSpecifiedCredentials { get => !this.model.UseDefaultCredentials; set => this.model.UseDefaultCredentials = !value; }

        [NotifiableProperty]
        public string Username { get => this.model.Username; set => this.model.Username = value; }

        [NotifiableProperty]
        public string Password { get => this.model.Password; set => this.model.Password = value; }

        public string DisplayName { get; set; } = "Email";

        public PackIconUniconsKind Icon => PackIconUniconsKind.At;
    }
}
