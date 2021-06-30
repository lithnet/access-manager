using System;
using System.Collections.Generic;
using System.Text;
using Lithnet.AccessManager.Server.Configuration;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Stylet;

namespace Lithnet.AccessManager.Server.UI
{
    public class MatchedSecurityDescriptorTargetViewModel : PropertyChangedBase
    {
        public SecurityDescriptorTargetViewModel Model { get; }

        public string DisplayName => this.Model.DisplayName;

        public TargetType Type => this.Model.Type;

        public string Description => this.Model.Description;

        public AccessMask EffectiveAccess { get; set; }

        public MatchedSecurityDescriptorTargetViewModel(SecurityDescriptorTargetViewModel model)
        {
            this.Model = model;
        }
    }
}
