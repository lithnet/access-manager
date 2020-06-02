using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.Laps.Web.Internal
{
    public enum AuthNFailureMessageID
    {
        UnknownFailure = 0,
        SsoIdentityNotFound = 1,
        ExternalAuthNProviderDenied = 2,
        ExternalAuthNProviderError = 3,
    }
}
