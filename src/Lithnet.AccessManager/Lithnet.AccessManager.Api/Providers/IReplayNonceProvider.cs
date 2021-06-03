using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Providers
{
    public interface IReplayNonceProvider
    {
        public string GenerateNonce();

        public bool ConsumeNonce(string nonce);
    }
}
