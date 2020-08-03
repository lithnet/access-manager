using System;
using System.DirectoryServices.ActiveDirectory;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.Logging;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ActiveDirectoryForestSchemaViewModel : PropertyChangedBase, IViewAware
    {
        private readonly ILogger<ActiveDirectoryForestSchemaViewModel> logger;

        public ActiveDirectoryForestSchemaViewModel(Forest forest, ILogger<ActiveDirectoryForestSchemaViewModel> logger)
        {
            this.Forest = forest;
            this.logger = logger;

            this.RefreshSchemaStatus();
        }

        public void RefreshSchemaStatus()
        {
            _ = this.PopulateLithnetSchemaStatus();
            _ = this.PopulateMsLapsSchemaStatus();
        }

        public string MsLapsSchemaPresentText { get; set; }

        public string LithnetAccessManagerSchemaPresentText { get; set; }

        public Forest Forest { get; }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }

        public UIElement View { get; set; }

        public string Name => this.Forest.Name;

        public bool IsLithnetSchemaPresent { get; set; }

        public bool IsNotLithnetSchemaPresent => !this.IsLithnetSchemaPresent;

        public bool IsMsLapsSchemaPresent { get; set; }

        public bool MsLapsSchemaLookupInProgress { get; set; }

        public bool LithnetSchemaLookupInProgress { get; set; }

        public bool IsNotMsLapsSchemaPresent => !this.IsMsLapsSchemaPresent;

        private async Task PopulateLithnetSchemaStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    this.LithnetSchemaLookupInProgress = true;
                    var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.Forest.Name));
                    schema.FindProperty("lithnetAdminPassword");
                    this.IsLithnetSchemaPresent = true;
                    this.LithnetAccessManagerSchemaPresentText = "Present";
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    this.IsLithnetSchemaPresent = false;
                    this.LithnetAccessManagerSchemaPresentText = "Not present";
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Could not determine Lithnet Access Manager schema status");
                    this.IsLithnetSchemaPresent = false;
                    this.LithnetAccessManagerSchemaPresentText = "Error looking up schema";
                }
                finally
                {
                    this.LithnetSchemaLookupInProgress = false;
                }
            }).ConfigureAwait(false);
        }

        private async Task PopulateMsLapsSchemaStatus()
        {
            await Task.Run(() =>
            {
                try
                {
                    this.MsLapsSchemaLookupInProgress = true;
                    var schema = ActiveDirectorySchema.GetSchema(new DirectoryContext(DirectoryContextType.Forest, this.Forest.Name));
                    schema.FindProperty("ms-Mcs-AdmPwd");
                    this.IsMsLapsSchemaPresent = true;
                    this.MsLapsSchemaPresentText = "Present";
                }
                catch (ActiveDirectoryObjectNotFoundException)
                {
                    this.IsMsLapsSchemaPresent = false;
                    this.MsLapsSchemaPresentText = "Not present";
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Could not determine Microsoft LAPS schema status");
                    this.IsMsLapsSchemaPresent = false;
                    this.MsLapsSchemaPresentText = "Error looking up schema";
                }
                finally
                {
                    this.MsLapsSchemaLookupInProgress = false;
                }
            }).ConfigureAwait(false);
        }
    }
}
