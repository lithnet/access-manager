using System.Collections.Generic;
using Lithnet.AccessManager.Configuration;
using Lithnet.AccessManager.Server;
using Lithnet.AccessManager.Web.AppSettings;
using Lithnet.AccessManager.Web.Internal;
using Microsoft.Extensions.Options;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class BuiltInAuthorizationService : IAuthorizationService
    {
        private readonly List<IAuthorizationService> enabledProviders;

        private readonly AuthorizationOptions options;

        public BuiltInAuthorizationService(IOptions<AuthorizationOptions> options, JsonTargetAuthorizationService jsonService, PowershellAuthorizationService psService)
        {
            this.enabledProviders = new List<IAuthorizationService>();
            this.options = options.Value;

            if (this.options.JsonProvider?.Enabled ?? false)
            {
                this.enabledProviders.Add(jsonService);
            }

            //if (this.options.PowershellProvider?.Enabled ?? false)
            //{
            //    this.enabledProviders.Add(psService);
            //}
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer, AccessMask requestedAccess)
        {
            AuthorizationResponse response = null;
            AuthorizationResponse summaryResponse = AuthorizationResponse.CreateAuthorizationResponse(requestedAccess);

            foreach (var provider in this.enabledProviders)
            {
                response = provider.GetAuthorizationResponse(user, computer, requestedAccess);

                if (response.IsExplicitResult())
                {
                    return response;
                }

                response?.NotificationChannels?.ForEach(t => summaryResponse?.NotificationChannels?.Add(t));
                if (summaryResponse.Code == AuthorizationResponseCode.Undefined && response != null)
                {
                    summaryResponse.Code = response.Code;
                }
            }

            if (summaryResponse.Code == AuthorizationResponseCode.Undefined)
            {
                summaryResponse.Code = AuthorizationResponseCode.NoMatchingRuleForComputer;
            }

            return summaryResponse;
        }
    }
}