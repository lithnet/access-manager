using System.Collections.Generic;
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

        public bool GetUsers(IViewAware owner, out List<SecurityIdentifier> sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetUsers(owner, targetServer, out sid);
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
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.ShowDialog(owner, scope, targetServer, out sid);
        }
        
        public bool GetUserOrServiceAccount (IViewAware owner, out SecurityIdentifier sid)
        {
            return this.GetUserOrServiceAccount(owner, null, out sid);
        }

        public bool GetUserOrServiceAccount(IViewAware owner, string targetServer, out SecurityIdentifier sid)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo
            {
                Filter = new DsFilterFlags
                {
                    UpLevel =
                    {
                        BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_SERVICE_ACCOUNTS
                    }
                },
                ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN,
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_SERVICE_ACCOUNTS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.ShowDialog(owner, scope, targetServer, out sid);
        }


        public bool GetUsers(IViewAware owner, string targetServer, out List<SecurityIdentifier> sid)
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
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.ShowDialog(owner, scope, targetServer, out sid);
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

            return this.ShowDialog(owner, scope, targetServer, out sid);
        }

        public bool GetGroups(IViewAware owner, out List<SecurityIdentifier> sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetGroups(owner, targetServer, out sid);
            }

            return false;
        }

        public bool GetGroups(IViewAware owner, string targetServer, out List<SecurityIdentifier> sid)
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

            return this.ShowDialog(owner, scope, targetServer, out sid);
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
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.ShowDialog(owner, scope, targetServer, out sid);
        }

        public bool GetComputers(IViewAware owner, out List<SecurityIdentifier> sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetComputers(owner, targetServer, out sid);
            }

            return false;
        }

        public bool GetComputers(IViewAware owner, string targetServer, out List<SecurityIdentifier> sid)
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
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE
            };

            return this.ShowDialog(owner, scope, targetServer, out sid);
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
                            BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS | DsopObjectFilterFlags.DSOP_FILTER_SERVICE_ACCOUNTS| DsopObjectFilterFlags.DSOP_FILTER_BUILTIN_GROUPS
                        }
                },
                ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN,
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE,
            };

            return this.ShowDialog(owner, scope, targetServer, out sid);
        }

        public bool GetUserOrGroups(IViewAware owner, out List<SecurityIdentifier> sid)
        {
            sid = null;

            if (this.PromptForTargetForest(owner, out string targetServer))
            {
                return this.GetUserOrGroups(owner, targetServer, out sid);
            }

            return false;
        }

        public bool GetUserOrGroups(IViewAware owner, string targetServer, out List<SecurityIdentifier> sids)
        {
            DsopScopeInitInfo scope = new DsopScopeInitInfo
            {
                Filter = new DsFilterFlags
                {
                    UpLevel =
                    {
                        BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS | DsopObjectFilterFlags.DSOP_FILTER_SERVICE_ACCOUNTS | DsopObjectFilterFlags.DSOP_FILTER_BUILTIN_GROUPS
                    }
                },
                ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE | DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN,
                InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_GROUPS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE,
            };

            return this.ShowDialog(owner, scope, targetServer, out sids);
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

        private bool ShowDialog(IViewAware owner, DsopScopeInitInfo scope, string targetServer, out SecurityIdentifier sid)
        {
            sid = null;

            DsopResult result = NativeMethods.ShowObjectPickerDialog(owner.GetHandle(), targetServer, DsopDialogInitializationOptions.DSOP_NONE, scope, "objectClass", "objectSid").FirstOrDefault();

            byte[] sidraw = result?.Attributes["objectSid"] as byte[];

            if (sidraw == null)
            {
                return false;
            }

            sid = new SecurityIdentifier(sidraw, 0);

            return true;
        }

        private bool ShowDialog(IViewAware owner, DsopScopeInitInfo scope, string targetServer, out List<SecurityIdentifier> sids)
        {
            sids = null;

            var results = NativeMethods.ShowObjectPickerDialog(owner.GetHandle(), targetServer, DsopDialogInitializationOptions.DSOP_FLAG_MULTISELECT, scope, "objectClass", "objectSid");

            if (results == null)
            {
                return false;
            }

            sids = new List<SecurityIdentifier>();

            foreach (var result in results)
            {
                byte[] sidraw = result?.Attributes["objectSid"] as byte[];

                if (sidraw != null)
                {
                    sids.Add(new SecurityIdentifier(sidraw, 0));
                }
            }

            return true;
        }
    }
}
