using Lithnet.AccessManager.Server.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuthorizationViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly AuthorizationOptions model;

        public AuthorizationViewModel(AuthorizationOptions model, SecurityDescriptorTargetsViewModelFactory factory)
        {
            this.model = model;
            this.Targets = factory.CreateViewModel(model.BuiltInProvider.Targets);
        }

        public SecurityDescriptorTargetsViewModel Targets { get; }

        public string DisplayName { get; set; } = "Authorization";
    }
}
