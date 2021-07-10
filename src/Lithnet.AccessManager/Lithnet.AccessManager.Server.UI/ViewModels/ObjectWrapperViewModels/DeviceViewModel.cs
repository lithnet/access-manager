using Microsoft.Extensions.Logging;
using Stylet;
using System;
using System.Windows;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class DeviceViewModel : Screen
    {

        public IDevice Model { get; }

        public DeviceViewModel(IDevice model)
        {
            this.Model = model;
            this.DisplayName = this.Model.DisplayName;
        }

        public string AgentVersion => this.Model.AgentVersion;

        public ApprovalState ApprovalState
        {
            get => this.Model.ApprovalState;
            set => this.Model.ApprovalState = value;
        }

        public bool IsApproved => this.ApprovalState == ApprovalState.Approved;

        public bool IsRejected => this.ApprovalState == ApprovalState.Rejected;

        public bool IsPending => this.ApprovalState == ApprovalState.Pending;

        public bool Disabled
        {
            get => this.Model.Disabled;
            set => this.Model.Disabled = value;
        }

        public bool Enabled => !this.Model.Disabled;
        
        public string AuthorityDeviceId => this.Model.AuthorityDeviceId;

        public string AuthorityId => this.Model.AuthorityId;

        public AuthorityType AuthorityType => this.Model.AuthorityType;

        public string ComputerName => this.Model.ComputerName;

        public DateTime Created => this.Model.Created.ToLocalTime();

        public DateTime Modified => this.Model.Modified.ToLocalTime();

        public string Description => this.Model.Description;

        public string DnsHostName => this.Model.DnsHostName;

        public string FullyQualifiedName => this.Model.FullyQualifiedName;

        public long Id => this.Model.Id;

        public string Name => this.Model.Name;

        public string ObjectID => this.Model.ObjectID;

        public string OperatingSystemFamily => this.Model.OperatingSystemFamily;

        public string OperatingSystemVersion => this.Model.OperatingSystemVersion;

        public string Sid => this.Model.Sid;
    }
}
