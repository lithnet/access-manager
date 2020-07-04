using System;
using System.Collections.Generic;
using System.Text;
using FluentValidation;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class PowershellNotificationChannelDefinitionValidator : AbstractValidator<PowershellNotificationChannelDefinitionViewModel>
    {
        public PowershellNotificationChannelDefinitionValidator()
        {
            this.RuleFor(r => r.DisplayName).NotEmpty();
            this.RuleFor(r => r.Script).Custom((item, context) =>
                {
                    if (string.IsNullOrWhiteSpace(item))
                    {
                        context.AddFailure("A script must be specified");
                    }
                    else if (!System.IO.File.Exists(item))
                    {
                        context.AddFailure("The file does not exist");
                    }
                });
        }
    }
}
