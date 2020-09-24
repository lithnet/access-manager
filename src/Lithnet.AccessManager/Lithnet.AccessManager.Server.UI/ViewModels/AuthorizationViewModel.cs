using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
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
        private readonly SecurityDescriptorTargetsViewModelFactory factory;
        private readonly IShellExecuteProvider shellExecuteProvider;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IImportTargetsViewModelFactory importTargetsFactory;
        private readonly IAuthorizationRuleImportProvider importProvider;
        private readonly ILogger logger;

        private ProgressDialogController progress;
        private int progressCurrent;
        private int progressMaximum;
        private CancellationTokenSource progressCts;

        public AuthorizationViewModel(AuthorizationOptions model, SecurityDescriptorTargetsViewModelFactory factory, IShellExecuteProvider shellExecuteProvider, IDialogCoordinator dialogCoordinator, IImportTargetsViewModelFactory importTargetsFactory, IAuthorizationRuleImportProvider importProvider, ILogger<AuthorizationViewModel> logger)
        {
            this.shellExecuteProvider = shellExecuteProvider;
            this.model = model;
            this.factory = factory;
            this.dialogCoordinator = dialogCoordinator;
            this.importTargetsFactory = importTargetsFactory;
            this.importProvider = importProvider;
            this.logger = logger;
            this.DisplayName = "Authorization";
        }

        protected override void OnInitialActivate()
        {
            Task.Run(() =>
            {
                this.Targets = this.factory.CreateViewModel(model.ComputerTargets);
            });
        }

        public SecurityDescriptorTargetsViewModel Targets { get; set; }

        public PackIconModernKind Icon => PackIconModernKind.Lock;

        public string HelpLink => Constants.HelpLinkPageAuthorization;

        public async Task Help()
        {
            await this.shellExecuteProvider.OpenWithShellExecute(this.HelpLink);
        }

        public async Task Import()
        {
            var vm = importTargetsFactory.CreateViewModel();

            ExternalDialogWindow window = new ExternalDialogWindow
            {
                Title = "Import authorization rules",
                DataContext = vm,
                SaveButtonVisible = true,
                CancelButtonName = "Close",
                SaveButtonName = "Import",
                Height = 600
            };

            if (window.ShowDialog() == false)
            {
                return;
            }

            await this.Import(vm);
        }

        private async Task Import(ImportSettingsViewModel settingsVm)
        {
            try
            {
                this.progress = await this.dialogCoordinator.ShowProgressAsync(this, "Importing...", "Discovering directory objects", true);
                this.progress.Canceled += Progress_Canceled;
                this.importProvider.OnStartProcessingComputer += ImportProvider_OnStartProcessingComputer;
                this.progress.SetIndeterminate();
                this.progressCts = new CancellationTokenSource();
                this.progressCurrent = 0;

                await Task.Run(() =>
                {
                    this.progressMaximum = this.importProvider.GetComputerCount(settingsVm.Target);
                    this.progress.Maximum = this.progressMaximum;
                });

                AuthorizationRuleImportSettings settings = new AuthorizationRuleImportSettings
                {
                    CancellationToken = this.progressCts.Token,
                    DoNotConsolidate = settingsVm.DoNotConsolidate,
                    DoNotConsolidateOnError = settingsVm.DoNotConsolidateOnError,
                    ImportFile = settingsVm.ImportFile,
                    ImportOU = settingsVm.Target,
                    HasHeaderRow = settingsVm.ImportFileHasHeaderRow,
                    DiscoveryMode = settingsVm.ImportType
                };

                var results = await Task.Run(() => this.importProvider.BuildPrincipalMap(settings));

                this.progress.SetIndeterminate();
                this.progress.SetMessage("Building authorization rules...");

                List<SecurityDescriptorTarget> targets = new List<SecurityDescriptorTarget>();
                this.PopulateTargets(results.MappedOU, settingsVm, targets);

                ImportResultsViewModel irvm = new ImportResultsViewModel();
                irvm.Targets = factory.CreateViewModel(targets);
                irvm.DiscoveryErrors = results.ComputerErrors;
                irvm.Targets.ChildDisplaySettings.IsScriptVisible = false;

                await this.progress.CloseAsync();

                ExternalDialogWindow window = new ExternalDialogWindow
                {
                    Title = "Validate authorization rules",
                    DataContext = irvm,
                    SaveButtonVisible = true,
                    CancelButtonIsDefault = false,
                    SaveButtonIsDefault = false,
                    CancelButtonName = "Cancel",
                    SaveButtonName = "Save and import rules",
                    Height = 600
                };

                if (window.ShowDialog() == false)
                {
                    return;
                }

                this.Merge(irvm.Targets, irvm.Merge, irvm.MergeOverwrite);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                this.logger.LogError(EventIDs.UIGenericError, ex, "Import error");
                await this.dialogCoordinator.ShowMessageAsync(this, "Import error", $"Could not perform the import. {ex.Message}");
            }
            finally
            {
                if (this.progress.IsOpen)
                {
                    await this.progress.CloseAsync();
                }
            }
        }

        private void Merge(SecurityDescriptorTargetsViewModel newTargets, bool merge, bool overwriteExisting)
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

        private void PopulateTargets(OUPrincipalMapping entry, ImportSettingsViewModel vm, List<SecurityDescriptorTarget> targets)
        {
            bool doNotConsolidate = vm.DoNotConsolidate || (vm.DoNotConsolidateOnError && entry.HasDescendantsWithErrors);

            if (!doNotConsolidate)
            {
                if (entry.UniquePrincipals.Count > 0)
                {
                    this.ConvertToTarget(entry, vm, targets);
                }
            }

            foreach (var computer in entry.Computers)
            {
                var admins = doNotConsolidate ? computer.Principals : computer.UniquePrincipals;

                if (!computer.HasError && admins.Count > 0)
                {
                    this.ConvertToTarget(computer.Sid, admins, vm, targets);
                }
            }

            foreach (var ou in entry.DescendantOUs)
            {
                this.PopulateTargets(ou, vm, targets);
            }
        }

        private void ConvertToTarget(SecurityIdentifier computerSid, HashSet<SecurityIdentifier> admins, ImportSettingsViewModel vm, List<SecurityDescriptorTarget> targets)
        {
            SecurityDescriptorTarget target = new SecurityDescriptorTarget()
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Description = vm.Description,
                Target = computerSid.ToString(),
                Type = TargetType.Computer,
                Id = Guid.NewGuid().ToString(),
                Notifications = vm.Notifications?.Model,
                Jit = new SecurityDescriptorTargetJitDetails()
                {
                    AuthorizingGroup = vm.JitAuthorizingGroup,
                    ExpireAfter = vm.JitExpireAfter
                },
                Laps = new SecurityDescriptorTargetLapsDetails()
                {
                    ExpireAfter = vm.LapsExpireAfter
                }
            };

            AccessMask mask = 0;
            mask |= vm.AllowLaps ? AccessMask.LocalAdminPassword : 0;
            mask |= vm.AllowJit ? AccessMask.Jit : 0;
            mask |= vm.AllowLapsHistory ? AccessMask.LocalAdminPasswordHistory : 0;
            mask |= vm.AllowBitlocker ? AccessMask.BitLocker : 0;

            DiscretionaryAcl acl = new DiscretionaryAcl(false, false, admins.Count);

            foreach (var sid in admins)
            {
                acl.AddAccess(AccessControlType.Allow, sid, (int)mask, InheritanceFlags.None, PropagationFlags.None);
            }

            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, acl);

            target.SecurityDescriptor = sd.GetSddlForm(AccessControlSections.All);

            targets.Add(target);
        }

        private void ConvertToTarget(OUPrincipalMapping entry, ImportSettingsViewModel vm, List<SecurityDescriptorTarget> targets)
        {
            SecurityDescriptorTarget target = new SecurityDescriptorTarget()
            {
                AuthorizationMode = AuthorizationMode.SecurityDescriptor,
                Description = vm.Description,
                Target = entry.OUName,
                Type = TargetType.Container,
                Id = Guid.NewGuid().ToString(),
                Notifications = vm.Notifications?.Model,
                Jit = new SecurityDescriptorTargetJitDetails()
                {
                    AuthorizingGroup = vm.JitAuthorizingGroup,
                    ExpireAfter = vm.JitExpireAfter
                },
                Laps = new SecurityDescriptorTargetLapsDetails()
                {
                    ExpireAfter = vm.LapsExpireAfter
                }
            };

            AccessMask mask = 0;
            mask |= vm.AllowLaps ? AccessMask.LocalAdminPassword : 0;
            mask |= vm.AllowJit ? AccessMask.Jit : 0;
            mask |= vm.AllowLapsHistory ? AccessMask.LocalAdminPasswordHistory : 0;
            mask |= vm.AllowBitlocker ? AccessMask.BitLocker : 0;

            DiscretionaryAcl acl = new DiscretionaryAcl(false, false, entry.UniquePrincipals.Count);

            foreach (var sid in entry.UniquePrincipals)
            {
                acl.AddAccess(AccessControlType.Allow, sid, (int)mask, InheritanceFlags.None, PropagationFlags.None);
            }

            CommonSecurityDescriptor sd = new CommonSecurityDescriptor(false, false, ControlFlags.DiscretionaryAclPresent, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), null, null, acl);

            target.SecurityDescriptor = sd.GetSddlForm(AccessControlSections.All);

            targets.Add(target);
        }

        private void Progress_Canceled(object sender, EventArgs e)
        {
            this.progressCts?.Cancel();
        }

        private void ImportProvider_OnStartProcessingComputer(object sender, ProcessingComputerArgs e)
        {
            this.progress.SetMessage($"Processing {e.ComputerName}");
            var val = Interlocked.Increment(ref this.progressCurrent);
            this.logger.LogTrace("Progress {count}/{max}", val, this.progressMaximum);
            this.progress.SetProgress(Math.Min(val, this.progressMaximum));
        }
    }
}
