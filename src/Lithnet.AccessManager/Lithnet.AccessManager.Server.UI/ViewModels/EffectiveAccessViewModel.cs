using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class EffectiveAccessViewModel : Screen, IHelpLink
    {
        private readonly IAuthorizationInformationBuilder authorizationBuilder;
        private readonly IDialogCoordinator dialogCoordinator;
        private readonly IDirectory directory;
        private readonly SecurityDescriptorTargetsViewModel targets;
        private readonly ILogger logger;
        private readonly IEnumerable<IComputerTargetProvider> computerTargetProviders;

        public string ComputerName { get; set; }

        public string Username { get; set; }

        public bool HasLaps { get; private set; }

        public bool HasLapsHistory { get; private set; }

        public bool HasBitLocker { get; private set; }

        public bool HasJit { get; private set; }

        public bool HasNoLaps { get; private set; }

        public bool HasNoLapsHistory { get; private set; }

        public bool HasNoBitLocker { get; private set; }

        public bool HasNoJit { get; private set; }

        public bool HasResults { get; private set; }

        public bool ShowMatchTable { get; private set; }

        public MatchedSecurityDescriptorTargetViewModel SelectedItem { get; set; }

        public ObservableCollection<MatchedSecurityDescriptorTargetViewModel> MatchedTargets { get; } = new ObservableCollection<MatchedSecurityDescriptorTargetViewModel>();

        public EffectiveAccessViewModel(IAuthorizationInformationBuilder authorizationBuilder, IDialogCoordinator dialogCoordinator, IDirectory directory, SecurityDescriptorTargetsViewModel targets, ILogger<EffectiveAccessViewModel> logger, IEnumerable<IComputerTargetProvider> computerTargetProviders)
        {
            this.authorizationBuilder = authorizationBuilder;
            this.dialogCoordinator = dialogCoordinator;
            this.directory = directory;
            this.targets = targets;
            this.computerTargetProviders = computerTargetProviders;
            this.logger = logger;
        }

        public bool CanCalculateEffectiveAccess => !string.IsNullOrWhiteSpace(this.Username) && !string.IsNullOrWhiteSpace(this.ComputerName);
        
        public async Task CalculateEffectiveAccess()
        {
            this.ClearResults();

            ProgressDialogController controller = null;
            MetroDialogSettings settings = new MetroDialogSettings
            {
                AnimateShow = false,
                AnimateHide = false,
            };

            if (string.IsNullOrWhiteSpace(this.Username))
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Enter a user name", "A user name must be provided", MessageDialogStyle.Affirmative, settings);
                return;
            }

            if (string.IsNullOrWhiteSpace(this.ComputerName))
            {
                await this.dialogCoordinator.ShowMessageAsync(this, "Enter a computer name", "A computer name must be provided", MessageDialogStyle.Affirmative, settings);
                return;
            }

            try
            {
                controller = await this.dialogCoordinator.ShowProgressAsync(this, "Evaluating access", "Evaluating access permissions...", false);
                controller.SetIndeterminate();
                AuthorizationContextProvider.DisableLocalFallback = true;

                await Task.Run(async () =>
                {
                    if (!this.directory.TryGetUser(this.Username, out IUser user))
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "Could not find user", "Could not find a matching user in the directory", MessageDialogStyle.Affirmative, settings);
                        return;
                    }

                    if (!this.directory.TryGetComputer(this.ComputerName, out IActiveDirectoryComputer computer))
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "Could not find computer", "Could not find a matching computer in the directory", MessageDialogStyle.Affirmative, settings);
                        return;
                    }

                    controller.SetMessage($"Evaluating access to computer {computer.MsDsPrincipalName} for user {user.MsDsPrincipalName}");

                    List<SecurityDescriptorTarget> matchingComputerTargets = new List<SecurityDescriptorTarget>();

                    foreach (var computerTargetProvider in this.computerTargetProviders)
                    {
                        if (computerTargetProvider.CanProcess(computer))
                        {
                            matchingComputerTargets.AddRange(await computerTargetProvider.GetMatchingTargetsForComputer(computer, targets.Model));
                        }
                    }

                    var results = await Task.Run(() => this.authorizationBuilder.BuildAuthorizationInformation(user, computer, matchingComputerTargets));

                    this.HasBitLocker = results.EffectiveAccess.HasFlag(AccessMask.BitLocker);
                    this.HasLaps = results.EffectiveAccess.HasFlag(AccessMask.LocalAdminPassword);
                    this.HasLapsHistory = results.EffectiveAccess.HasFlag(AccessMask.LocalAdminPasswordHistory);
                    this.HasJit = results.EffectiveAccess.HasFlag(AccessMask.Jit);
                    this.HasNoBitLocker = !this.HasBitLocker;
                    this.HasNoLaps = !this.HasLaps;
                    this.HasNoLapsHistory = !this.HasLapsHistory;
                    this.HasNoJit = !this.HasJit;

                    Dictionary<string, MatchedSecurityDescriptorTargetViewModel> items = new Dictionary<string, MatchedSecurityDescriptorTargetViewModel>(StringComparer.OrdinalIgnoreCase);

                    this.MergeResults(results.SuccessfulBitLockerTargets, AccessMask.BitLocker, items);
                    this.MergeResults(results.SuccessfulJitTargets, AccessMask.Jit, items);
                    this.MergeResults(results.SuccessfulLapsHistoryTargets, AccessMask.LocalAdminPasswordHistory, items);
                    this.MergeResults(results.SuccessfulLapsTargets, AccessMask.LocalAdminPassword, items);

                    foreach (var item in items.Values)
                    {
                        await Execute.OnUIThreadAsync(() => this.MatchedTargets.Add(item));
                    }

                    this.ShowMatchTable = items.Count > 0;
                    this.HasResults = true;
                });
            }
            catch (Exception ex)
            {
                this.ClearResults();
                this.logger.LogError(EventIDs.UIGenericError, ex, "Unable to calculate effective permissions");

                if (ex.InnerException is Win32Exception wx)
                {
                    if (wx.ErrorCode == 5)
                    {
                        await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"Access denied. Ensure you are a member of the 'Windows Authorization Access Group' in the user and computer domains, and if either is located in another forest, you must be a member of the 'Access Control Assistance Operators' group in that forest");
                    }
                }

                await this.dialogCoordinator.ShowMessageAsync(this, "Error", $"There was a problem calculating the effective permissions. {ex.Message}");
            }
            finally
            {
                if (controller != null)
                {
                    if (controller.IsOpen)
                    {
                        await controller.CloseAsync();
                    }
                }
            }
        }

        public bool CanClearResults => this.HasResults;

        public void ClearResults()
        {
            this.HasBitLocker = false;
            this.HasLaps = false;
            this.HasLapsHistory = false;
            this.HasJit = false;
            this.HasNoBitLocker = false;
            this.HasNoLaps = false;
            this.HasNoLapsHistory = false;
            this.HasNoJit = false;
            this.MatchedTargets.Clear();
            this.HasResults = false;
            this.ShowMatchTable = false;
        }

        public bool CanEdit => this.SelectedItem != null;

        public async Task Edit()
        {
            await this.targets.EditItem(this.SelectedItem?.Model, this.GetWindow());
        }

        public async Task ComputerNameTextBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await this.CalculateEffectiveAccess();
            }
        }

        private void MergeResults(IList<SecurityDescriptorTarget> matchedTargets, AccessMask mask, Dictionary<string, MatchedSecurityDescriptorTargetViewModel> items)
        {
            foreach (var item in matchedTargets)
            {
                if (items.ContainsKey(item.Id))
                {
                    items[item.Id].EffectiveAccess |= mask;
                }
                else
                {
                    var existingvm = this.targets.ViewModels.FirstOrDefault(t => t.Id == item.Id);

                    if (existingvm != null)
                    {
                        items.Add(item.Id, new MatchedSecurityDescriptorTargetViewModel(existingvm) { EffectiveAccess = mask });
                    }
                }
            }
        }

        public string HelpLink => Constants.HelpLinkPageEffectiveAccess;
    }
}
