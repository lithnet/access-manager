using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class RegistrationKeyViewModel : ValidatingModelBase, IViewAware
    {
        private readonly ILogger<RegistrationKeyViewModel> logger;

        public IRegistrationKey Model { get; }

        public UIElement View { get; set; }

        public RegistrationKeyViewModel(IRegistrationKey model, ILogger<RegistrationKeyViewModel> logger, IModelValidator<RegistrationKeyViewModel> validator)
        {
            this.logger = logger;
            this.Model = model;
            this.Validator = validator;
            this.Validate();
        }

        public string Key
        {
            get => this.Model.Key;
            set => this.Model.Key = value;
        }

        public int ActivationCount
        {
            get => this.Model.ActivationCount;
            set => this.Model.ActivationCount = value;
        }

        public bool IsActivationLimited
        {
            get => this.ActivationLimit > 0;
            set => this.ActivationLimit = value ? 1 : 0;
        }

        public string ActivationLimitDescription => this.ActivationLimit > 0 ? this.ActivationLimit.ToString() : "No limit";

        public int ActivationLimit
        {
            get => this.Model.ActivationLimit;
            set => this.Model.ActivationLimit = value;
        }

        public bool Enabled
        {
            get => this.Model.Enabled;
            set => this.Model.Enabled = value;
        }

        public string Name
        {
            get => this.Model.Name;
            set => this.Model.Name = value;
        }

        public void ResetActivationCount()
        {
            this.ActivationCount = 0;
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}
