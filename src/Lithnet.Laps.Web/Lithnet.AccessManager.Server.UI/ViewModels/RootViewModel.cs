using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Newtonsoft.Json;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class RootViewModel : Screen
    {
        public string User { get; set; }

        public string Container { get; set; }

        public ApplicationConfigViewModel Config { get; set; }

        public RootViewModel(IApplicationConfig config)
        {
            this.DisplayName = "Lithnet Access Manager Service Configuration";
            this.Config = new ApplicationConfigViewModel(config);
        }

        public void LoadFile(string path)
        {
            string data = System.IO.File.ReadAllText(path);
            //this.Config = JsonConvert.DeserializeObject<ApplicationConfig>(data);
        }

        public void ShowObjectPicker()
        {

            Window window = Window.GetWindow(this.View);
            var wih = new WindowInteropHelper(window);
            IntPtr hWnd = wih.Handle;

            var results = NativeMethods.ShowObjectPickerDialog(hWnd, "objectSid", "msDS-PrincipalName");

            if (results != null)
            {
                var result = results.First();

               var sid =  new SecurityIdentifier(result.Attributes["objectSid"] as byte[], 0).ToString();
                var pname = result.Attributes["msDS-PrincipalName"] as string;

                this.User = $"{sid} - {pname} - {result.AdsPath}";
            }
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
