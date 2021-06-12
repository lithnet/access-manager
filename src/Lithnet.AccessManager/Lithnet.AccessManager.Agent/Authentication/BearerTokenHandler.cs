using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Authentication
{
    public class BearerTokenHandler : DelegatingHandler
    {
        private readonly ITokenProvider tokenProvider;

        public BearerTokenHandler(ITokenProvider tokenProvider)
        {
            this.tokenProvider = tokenProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var token = await this.tokenProvider.GetAccessToken();
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
