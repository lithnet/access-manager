using System.Collections.Generic;

namespace Lithnet.AccessManager.Server
{
    public interface IPhoneticPasswordTextProvider
    {
        IEnumerable<string> GetPhoneticTextSections(string password);

        string GetPhoneticText(string password);
    }
}