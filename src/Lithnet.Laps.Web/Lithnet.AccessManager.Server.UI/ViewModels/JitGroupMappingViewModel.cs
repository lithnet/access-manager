using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.AccessManager.Server.UI.Interop;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public sealed class JitGroupMappingViewModel : ValidatingModelBase, IViewAware
    {
        public JitGroupMapping Model { get; }

        public UIElement View { get; set; }

        public JitGroupMappingViewModel(JitGroupMapping model, IModelValidator<JitGroupMappingViewModel> validator)
        {
            this.Model = model;
            this.Validator = validator;
        }

        public string ComputerOU
        {
            get => this.Model.ComputerOU;
            set => this.Model.ComputerOU = value;
        }

        public string GroupOU
        {
            get => this.Model.GroupOU;
            set => this.Model.GroupOU = value;
        }

        public string GroupNameTemplate
        {
            get => this.Model.GroupNameTemplate;
            set => this.Model.GroupNameTemplate = value;
        }

        public bool EnableJitGroupDeletion
        {
            get => this.Model.EnableJitGroupDeletion;
            set => this.Model.EnableJitGroupDeletion = value;
        }

        public bool Subtree
        {
            get => this.Model.Subtree;
            set => this.Model.Subtree = value;
        }

        public bool OneLevel
        {
            get => !this.Model.Subtree;
            set => this.Model.Subtree = !value;
        }

        public GroupType GroupType
        {
            get => this.Model.GroupType;
            set => this.Model.GroupType = value;
        }

        public IEnumerable<GroupType> GroupTypeValues => Enum.GetValues(typeof(GroupType)).Cast<GroupType>();

        public void SelectComputerOU()
        {
            var container = NativeMethods.ShowContainerDialog(this.GetHandle(), "Select OU", "Select computer OU");
            if (container != null)
            {
                this.ComputerOU = container;
            }
        }

        public void SelectGroupOU()
        {
            var container = NativeMethods.ShowContainerDialog(this.GetHandle(), "Select OU", "Select group OU");
            if (container != null)
            {
                this.GroupOU = container;
            }
        }

        public void AttachView(UIElement view)
        {
            this.View = view;
        }
    }
}
