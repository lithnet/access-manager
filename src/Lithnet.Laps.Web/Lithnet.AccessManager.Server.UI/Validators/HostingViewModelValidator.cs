using System.IO;
using FluentValidation;

namespace Lithnet.AccessManager.Server.UI
{
    public class HostingViewModelValidator : AbstractValidator<HostingViewModel>
    {
        public HostingViewModelValidator(IAppPathProvider appPathProvider)
        {
            this.RuleFor(r => r.Certificate)
                .NotEmpty()
                .WithMessage("A HTTPS certificate must be provided");

            this.RuleFor(t => t.ServiceAccount).NotEmpty().WithMessage("A service account must be specified");
            this.RuleFor(t => t.HttpPort).NotEmpty().WithMessage("A HTTP port must be specified");
            this.RuleFor(t => t.HttpsPort).NotEmpty().WithMessage("A HTTPS port must be specified");


        }
    }
}
