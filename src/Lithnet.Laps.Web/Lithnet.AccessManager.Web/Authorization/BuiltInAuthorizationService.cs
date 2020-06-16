using System.Collections.Generic;
using Lithnet.AccessManager.Web.AppSettings;
using Lithnet.AccessManager.Web.Internal;

namespace Lithnet.AccessManager.Web.Authorization
{
    public class BuiltInAuthorizationService : IAuthorizationService
    {
        private readonly List<IAuthorizationService> enabledProviders;

        public BuiltInAuthorizationService(IAuthorizationSettings config, JsonTargetAuthorizationService jsonService, PowershellAuthorizationService psService)
        {
            this.enabledProviders = new List<IAuthorizationService>();

            if (config.JsonProviderEnabled)
            {
                this.enabledProviders.Add(jsonService);
            }

            if (config.PowershellProviderEnabled)
            {
                this.enabledProviders.Add(psService);
            }
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

                response?.NotificationChannels?.ForEach(t => summaryResponse.NotificationChannels.Add(t));
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