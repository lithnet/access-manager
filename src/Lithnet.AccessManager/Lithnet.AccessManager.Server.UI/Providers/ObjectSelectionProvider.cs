using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class ObjectSelectionProvider : IObjectSelectionProvider
    {
        private readonly IDiscoveryServices discoveryServices;
        private readonly IDomainTrustProvider domainTrustProvider;

        public ObjectSelectionProvider(IDiscoveryServices discoveryServices, IDomainTrustProvider domainTrustProvider)
        {
            this.discoveryServices = discoveryServices;
            this.domainTrustProvider = domainTrustProvider;
        }

        public bool SelectContainer(IViewAware owner, string dialogTitle, string treeViewTitle, string baseContainer, string selectedContainer, out string container)
        {
            container = NativeMethods.ShowContainerDialog(owner.GetHandle(), "Select OU", "Select computer OU", baseContainer, selectedContainer);

            return container != null;
        }

        public bool GetUser(IViewAware owner, out SecurityIdentifier sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetUser(owner, targetServer, out sid);
            }

            return false;
        }

        public bool GetUser(IViewAware owner, string targetServer, out SecurityIdentifier sid)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo
            {
                Filter = new DsFilterFlags
                {
                    UpLevel =
                    {
                        BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_USERS
                    }
                },
                ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN,
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.GetUserOrGroup(owner, scope, targetServer, out sid);
        }

        public bool GetGroup(IViewAware owner, out SecurityIdentifier sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetGroup(owner, targetServer, out sid);
            }

            return false;
        }

        public bool GetGroup(IViewAware owner, string targetServer, out SecurityIdentifier sid)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo
            {
                Filter = new DsFilterFlags
                {
                    UpLevel =
                    {
                        BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE
                    }
                },
                ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN,
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.GetUserOrGroup(owner, scope, targetServer, out sid);
        }

        public bool GetComputer(IViewAware owner, out SecurityIdentifier sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetComputer(owner, targetServer, out sid);
            }

            return false;
        }

        public bool GetComputer(IViewAware owner, string targetServer, out SecurityIdentifier sid)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo
            {
                Filter = new DsFilterFlags
                {
                    UpLevel =
                    {
                        BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_COMPUTERS
                    }
                },
                ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN,
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.GetUserOrGroup(owner, scope, targetServer, out sid);
        }

        public bool GetUserOrGroup(IViewAware owner, out SecurityIdentifier sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetUserOrGroup(owner, targetServer, out sid);
            }

            return false;
        }

        public bool GetUserOrGroup(IViewAware owner, string targetServer, out SecurityIdentifier sid)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo
            {
                Filter = new DsFilterFlags
                {
                    UpLevel =
                        {
                            BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS
                        }
                },
                ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN,
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.GetUserOrGroup(owner, scope, targetServer, out sid);
        }

        private bool PromptForTargetForest(IViewAware owner, out string targetServer)
        {
            targetServer = null;

            SelectForestViewModel vm = new SelectForestViewModel();

            ExternalDialogWindow w = new ExternalDialogWindow
            {
                Title = "Select forest",
                DataContext = vm,
                SaveButtonName = "Next...",
                SizeToContent = SizeToContent.WidthAndHeight,
                SaveButtonIsDefault = true
            };

            foreach (Forest forest in this.domainTrustProvider.GetForests())
            {
                vm.AvailableForests.Add(forest.Name);
            }

            vm.SelectedForest = vm.AvailableForests.FirstOrDefault();

            if (vm.AvailableForests.Count > 1)
            {
                w.Owner = owner.GetWindow();

                if (!w.ShowDialog() ?? false)
                {
                    return false;
                }
            }

            targetServer = this.discoveryServices.GetDomainController(vm.SelectedForest ?? Forest.GetCurrentForest().Name);

            return true;
        }

        private bool GetUserOrGroup(IViewAware owner, DsopScopeInitInfo scope, string targetServer, out SecurityIdentifier sid)
        {
            sid = null;

            DsopResult result = NativeMethods.ShowObjectPickerDialog(owner.GetHandle(), targetServer, scope, "objectClass", "objectSid").FirstOrDefault();

            byte[] sidraw = result?.Attributes["objectSid"] as byte[];

            if (sidraw == null)
            {
                return false;
            }

            sid = new SecurityIdentifier(sidraw, 0);

            return true;
        }
    }
}
