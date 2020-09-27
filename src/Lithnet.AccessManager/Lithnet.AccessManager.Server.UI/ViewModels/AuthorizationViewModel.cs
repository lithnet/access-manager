using System;
using System.Linq;
using System.Security.AccessControl;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.IconPacks;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class AuthorizationViewModel : Screen, IHelpLink
    {
        private readonly AuthorizationOptions model;
        private readonly SecurityDescriptorTargetsViewModelFactory targetViewModelFactory;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly ILogger logger;
        private readonly Func<ImportWizardWindowViewModel> importWizardFactory;
        private readonly IWindowManager windowManager;

        public AuthorizationViewModel(AuthorizationOptions model, SecurityDescriptorTargetsViewModelFactory targetViewModelFactory, IShellExecuteProvider shellExecuteProvider, IDialogCoordinator dialogCoordinator, ILogger<AuthorizationViewModel> logger, Func<ImportWizardWindowViewModel> importWizardFactory, IWindowManager windowManager)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.model = model;
            this.targetViewModelFactory = targetViewModelFactory;
            this.logger = logger;
            this.importWizardFactory = importWizardFactory;
            this.windowManager = windowManager;
            this.DisplayName = "Authorization";
        }

        protected override void OnInitialActivate()
        {
            Task.Run(async () =>
            {
                this.Targets = await this.targetViewModelFactory.CreateViewModelAsync(model.ComputerTargets);
                this.IsLoading = false;
            });
        }

        public SecurityDescriptorTargetsViewModel Targets { get; set; }

        public PackIconModernKind Icon => PackIconModernKind.Lock;

        public bool IsLoading { get; set; } = true;

        public bool HasLoaded => !this.IsLoading;

        public string HelpLink => Constants.HelpLinkPageAuthorization;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public void Import()
        {
            var vm = importWizardFactory.Invoke();
            vm.AuthorizationViewModel = this;

            windowManager.ShowDialog(vm);
        }

        public void Merge(SecurityDescriptorTargetsViewModel newTargets, bool merge, bool overwriteExisting)
        {
            foreach (var newTarget in newTargets.ViewModels)
            {
                if (!merge)
                {
                    Execute.OnUIThread(() => this.Targets.ViewModels.Add(newTarget));
                    this.Targets.Model.Add(newTarget.Model);
                    continue;
                }

                var existingTarget = this.Targets.ViewModels.FirstOrDefault(t => t.IsModePermission && string.Equals(t.Target, newTarget.Target, StringComparison.OrdinalIgnoreCase));

                if (existingTarget == null)
                {
                    Execute.OnUIThread(() => this.Targets.ViewModels.Add(newTarget));
                    continue;
                }

                if (string.IsNullOrWhiteSpace(existingTarget.JitAuthorizingGroup) || overwriteExisting)
                {
                    if (!string.IsNullOrWhiteSpace(newTarget.JitAuthorizingGroup))
                    {
                        existingTarget.JitAuthorizingGroup = newTarget.JitAuthorizingGroup;
                    }
                }

                if (existingTarget.JitExpireMinutes == 0 || overwriteExisting)
                {
                    if (newTarget.JitExpireMinutes > 0)
                    {
                        existingTarget.JitExpireAfter = newTarget.JitExpireAfter;
                    }
                }

                if (existingTarget.LapsExpireMinutes == 0 || overwriteExisting)
                {
                    if (newTarget.LapsExpireMinutes > 0)
                    {
                        existingTarget.LapsExpireAfter = newTarget.LapsExpireAfter;
                    }
                }

                if (string.IsNullOrWhiteSpace(existingTarget.Description) || overwriteExisting)
                {
                    if (!string.IsNullOrWhiteSpace(newTarget.Description))
                    {
                        existingTarget.Description = newTarget.Description;
                    }
                }

                if (overwriteExisting && newTarget.Notifications.SuccessSubscriptions.Count > 0)
                {
                    existingTarget.Notifications.SuccessSubscriptions.Clear();
                    existingTarget.Notifications.Model.OnSuccess.Clear();
                }

                foreach (var notification in newTarget.Notifications.SuccessSubscriptions)
                {
                    if (existingTarget.Notifications.SuccessSubscriptions.All(t => t.Id != notification.Id))
                    {
                        existingTarget.Notifications.SuccessSubscriptions.Add(notification);
                        existingTarget.Notifications.Model.OnSuccess.Add(notification.Id);
                    }
                }


                if (overwriteExisting && newTarget.Notifications.FailureSubscriptions.Count > 0)
                {
                    existingTarget.Notifications.FailureSubscriptions.Clear();
                    existingTarget.Notifications.Model.OnFailure.Clear();
                }

                foreach (var notification in newTarget.Notifications.FailureSubscriptions)
                {
                    if (existingTarget.Notifications.FailureSubscriptions.All(t => t.Id != notification.Id))
                    {
                        existingTarget.Notifications.FailureSubscriptions.Add(notification);
                        existingTarget.Notifications.Model.OnFailure.Add(notification.Id);
                    }
                }

                RawSecurityDescriptor existingrsd = new RawSecurityDescriptor(existingTarget.SecurityDescriptor);
                RawSecurityDescriptor newrsd = new RawSecurityDescriptor(newTarget.SecurityDescriptor);
                CommonSecurityDescriptor existingsd = new CommonSecurityDescriptor(false, false, existingrsd);
                CommonSecurityDescriptor newsd = new CommonSecurityDescriptor(false, false, newrsd);

                foreach (var ace in newsd.DiscretionaryAcl.OfType<CommonAce>())
                {
                    existingsd.DiscretionaryAcl.AddAccess((AccessControlType)ace.AceType, ace.SecurityIdentifier, ace.AccessMask, ace.InheritanceFlags, ace.PropagationFlags);
                }

                existingTarget.SecurityDescriptor = existingsd.GetSddlForm(AccessControlSections.All);
            }
        }
    }
}
