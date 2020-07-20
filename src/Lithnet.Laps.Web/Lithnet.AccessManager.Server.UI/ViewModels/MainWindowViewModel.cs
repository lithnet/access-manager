using System.Security.AccessControl;
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
