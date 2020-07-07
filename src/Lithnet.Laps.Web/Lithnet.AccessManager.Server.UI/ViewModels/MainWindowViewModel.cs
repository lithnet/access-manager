using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using System.Windows.Interop;
using Community.Windows.Forms;
using Lithnet.AccessManager.Server.UI.Interop;
using Lithnet.AccessManager.Server.UI.Providers;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class MainWindowViewModel : Screen
    {
        public ApplicationConfigViewModel Config { get; set; }

        public MainWindowViewModel(ApplicationConfigViewModel c)
        {
            this.DisplayName = "Lithnet Admin Access Service Configuration";
            this.Config = c;
        }

        public void Test()
        {
            AccessControlEditorDialog dialog = new AccessControlEditorDialog();

            dialog.PageType = Community.Security.AccessControl.SecurityPageType.BasicPermissions;
            dialog.AllowEditOwner = false;
            dialog.AllowEditAudit = false;
            dialog.AllowDaclInheritanceReset = false;
            dialog.AllowSaclInheritanceReset = false;
            dialog.ViewOnly = false;

            var provider = new AdminAccessTargetProvider();
            RawSecurityDescriptor sd = new RawSecurityDescriptor("D:(A;;0x200;;;S-1-5-11)");
            byte[] sdBytes = new byte[sd.BinaryLength];
            sd.GetBinaryForm(sdBytes, 0);
            dialog.Initialize("My test object", "This is my test object", false, provider, sdBytes);
            var r =  dialog.ShowDialog();

            var result = dialog.SDDL;
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
    }
}
