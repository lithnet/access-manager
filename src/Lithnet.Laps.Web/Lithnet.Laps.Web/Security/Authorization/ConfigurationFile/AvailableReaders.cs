using System.Collections.Generic;
using System.Linq;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile
{
    public class AvailableReaders: IAvailableReaders
    {
        private readonly ILapsConfig configSection;

        public AvailableReaders(ILapsConfig configSection)
        {
            this.configSection = configSection;
        }

        public IEnumerable<IReaderElement> GetReadersForTarget(ITarget target)
        {
            TargetCollection targetElementCollection = this.configSection.Targets;

            IEnumerable<ReaderCollection> query = from targetElement in targetElementCollection.OfType<TargetElement>()
                where targetElement.Name == target.TargetName
                select targetElement.Readers;

            ReaderCollection readerCollection = query.FirstOrDefault();

            if (readerCollection == null)
            {
                return new ReaderElement[0];
            }

            return readerCollection.OfType<ReaderElement>();
        }
    }
}