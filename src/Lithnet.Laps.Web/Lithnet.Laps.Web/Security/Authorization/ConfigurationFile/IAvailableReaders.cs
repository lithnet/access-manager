using System.Collections.Generic;
using Lithnet.Laps.Web.Models;

namespace Lithnet.Laps.Web.Security.Authorization.ConfigurationFile
{
    public interface IAvailableReaders
    {
        IEnumerable<IReaderElement> GetReadersForTarget(ITarget target);
    }
}
