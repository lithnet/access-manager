using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;
using Lithnet.AccessManager.Configuration;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class UserInterfaceViewModel : PropertyChangedBase, IHaveDisplayName
    {
        private readonly UserInterfaceOptions model;

        public UserInterfaceViewModel(UserInterfaceOptions model)
        {
            this.model = model;
        }

        public bool AllowJit { get => this.model.AllowJit; set => this.model.AllowJit = value; }

        public bool AllowLaps { get => this.model.AllowLaps; set => this.model.AllowLaps = value; }

        public bool AllowLapsHistory { get => this.model.AllowLapsHistory; set => this.model.AllowLapsHistory = value; }

        public string Title { get => this.model.Title; set => this.model.Title = value; }

        public AuditReasonFieldState UserSuppliedReason { get => this.model.UserSuppliedReason; set => this.model.UserSuppliedReason = value; }
        
        public IEnumerable<AuditReasonFieldState> UserSuppliedReasonValues
        {
            get
            {
                return Enum.GetValues(typeof(AuditReasonFieldState)).Cast<AuditReasonFieldState>();
            }
        }

        public BitmapImage Image
        {
            get
            {
                string path = @"D:\dev\git\lithnet\laps-web\src\Lithnet.Laps.Web\Lithnet.AccessManager.Web\wwwroot\images\logo.png";
                return new BitmapImage(new Uri(path));
            }
        }

        public void FindImage()
        {

        }
 
        public string DisplayName { get; set; } = "User interface";
    }
}
