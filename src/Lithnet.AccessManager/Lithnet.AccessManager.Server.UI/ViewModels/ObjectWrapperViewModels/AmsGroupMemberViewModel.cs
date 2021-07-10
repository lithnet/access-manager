namespace Lithnet.AccessManager.Server.UI
{
    public class AmsGroupMemberViewModel : IAmsGroupMemberViewModel
    {
        public object Model => this.model;

        private IDevice model;

        public AmsGroupMemberViewModel(IDevice device)
        {
            this.model = device;
        }

        public string DisplayName => this.model.DisplayName;

        public string Type => "Device";

        public string Sid => this.model.Sid;
    }
}