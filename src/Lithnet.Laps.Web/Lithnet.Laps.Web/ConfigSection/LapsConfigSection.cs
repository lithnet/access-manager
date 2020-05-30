using System.Configuration;
using Lithnet.Laps.Web.Audit;
using Lithnet.Laps.Web.Config;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web
{
    public sealed class LapsConfigSection : ConfigurationSection, ILapsConfig
    {
        public const string SectionName = "lithnet-laps";
        private const string PropTargets = "targets";
        private const string PropTarget = "target";
        
        [ConfigurationProperty(LapsConfigSection.PropTargets)]
        [ConfigurationCollection(typeof(TargetCollection), AddItemName = LapsConfigSection.PropTarget, CollectionType = ConfigurationElementCollectionType.BasicMap)]
        public TargetCollection Targets => (TargetCollection)this[LapsConfigSection.PropTargets];
    }
}