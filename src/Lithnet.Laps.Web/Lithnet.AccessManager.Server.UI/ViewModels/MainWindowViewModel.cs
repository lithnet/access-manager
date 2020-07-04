using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Interop;
using Lithnet.AccessManager.Server.UI.Interop;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class MainWindowViewModel : Screen
    {
        public string User { get; set; }

        public string Container { get; set; }

        public ApplicationConfigViewModel Config { get; set; }

        public MainWindowViewModel(ApplicationConfigViewModel c)
        {
            this.DisplayName = "Lithnet Admin Access Service Configuration";
            this.Config = c;
        }

        public void Save()
        {
            this.Config.Save();
        }

        public void Close()
        {
            this.RequestClose();
        }

        public void Help()
        {

        }

        public void About()
        {

        }

        public void ShowObjectPicker()
        {

            Window window = Window.GetWindow(this.View);
            var wih = new WindowInteropHelper(window);
            IntPtr hWnd = wih.Handle;

            var results = NativeMethods.ShowObjectPickerDialog(hWnd, this.GetScopes(), "objectSid", "msDS-PrincipalName");

            if (results != null)
            {
                var result = results.First();

                var sid = new SecurityIdentifier(result.Attributes["objectSid"] as byte[], 0).ToString();
                var pname = result.Attributes["msDS-PrincipalName"] as string;

                this.User = $"{sid} - {pname} - {result.AdsPath}";
            }
        }

        private List<DsopScopeInitInfo> GetScopes()
        {
            List<DsopScopeInitInfo> list = new List<DsopScopeInitInfo>();

            DsopScopeInitInfo scope = new DsopScopeInitInfo();

            scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_ENTERPRISE_DOMAIN;
            scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_STARTING_SCOPE | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS;
            scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_SERVICE_ACCOUNTS | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_BUILTIN_GROUPS | DsopObjectFilterFlags.DSOP_FILTER_COMPUTERS;

            list.Add(scope);

            scope = new DsopScopeInitInfo();
            scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_GLOBAL_CATALOG;
            scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS;
            scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_SERVICE_ACCOUNTS | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_BUILTIN_GROUPS | DsopObjectFilterFlags.DSOP_FILTER_COMPUTERS;


            list.Add(scope);

            scope = new DsopScopeInitInfo();
            scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_EXTERNAL_UPLEVEL_DOMAIN;
            scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS;
            scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_SERVICE_ACCOUNTS | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_BUILTIN_GROUPS | DsopObjectFilterFlags.DSOP_FILTER_COMPUTERS;


            list.Add(scope);

            scope = new DsopScopeInitInfo();
            scope.ScopeType = DsopScopeTypeFlags.DSOP_SCOPE_TYPE_USER_ENTERED_UPLEVEL_SCOPE;
            scope.InitInfo = DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_COMPUTERS | DsopScopeInitInfoFlags.DSOP_SCOPE_FLAG_DEFAULT_FILTER_USERS;
            scope.Filter.UpLevel.BothModeFilter = DsopObjectFilterFlags.DSOP_FILTER_USERS | DsopObjectFilterFlags.DSOP_FILTER_WELL_KNOWN_PRINCIPALS | DsopObjectFilterFlags.DSOP_FILTER_UNIVERSAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_SERVICE_ACCOUNTS | DsopObjectFilterFlags.DSOP_FILTER_GLOBAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_DOMAIN_LOCAL_GROUPS_SE | DsopObjectFilterFlags.DSOP_FILTER_BUILTIN_GROUPS | DsopObjectFilterFlags.DSOP_FILTER_COMPUTERS;

            list.Add(scope);

            return list;
        }

        public void ShowContainerPicker()
        {
            Window window = Window.GetWindow(this.View);
            var wih = new WindowInteropHelper(window);
            IntPtr hWnd = wih.Handle;

            var result = NativeMethods.ShowContainerDialog(hWnd);

            if (result != null)
            {
                this.Container = result;
            }
        }
    }
}
