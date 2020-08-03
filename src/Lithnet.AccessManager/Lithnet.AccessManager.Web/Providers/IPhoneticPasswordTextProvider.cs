using System.Collections.Generic;

namespace Lithnet.AccessManager.Web.Internal
{
    public interface IPhoneticPasswordTextProvider
    {
        IEnumerable<string> GetPhoneticText(string password);
    }
}