using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.AppSettings;
using Microsoft.Ajax.Utilities;

namespace Lithnet.Laps.Web.Authorization
{
    public class BuiltInAuthorizationService : IAuthorizationService
    {
        private readonly IAuthorizationSettings config;
        private readonly JsonTargetAuthorizationService jsonService;
        private readonly PowershellAuthorizationService psService;

        public BuiltInAuthorizationService(IAuthorizationSettings config, JsonTargetAuthorizationService jsonService, PowershellAuthorizationService psService)
        {
            this.config = config;
            this.jsonService = jsonService;
            this.psService = psService;
        }

        public AuthorizationResponse GetAuthorizationResponse(IUser user, IComputer computer)
        {
            AuthorizationResponse jsonResponse = null;
            AuthorizationResponse psResponse = null;

            if (config.JsonProviderEnabled)
            {
                jsonResponse = this.jsonService.GetAuthorizationResponse(user, computer);

                if (jsonResponse.IsExplicitResult())
                {
                    return jsonResponse;
                }
            }

            if (config.PowershellProviderEnabled)
            {
                psResponse = this.psService.GetAuthorizationResponse(user, computer);

                if (psResponse.IsExplicitResult())
                {
                    return psResponse;
                }
            }

            AuthorizationResponse summaryResponse = new AuthorizationResponse();
            jsonResponse?.NotificationRecipients?.ForEach(t => summaryResponse.NotificationRecipients.Add(t));
            psResponse?.NotificationRecipients?.ForEach(t => summaryResponse.NotificationRecipients.Add(t));
            summaryResponse.Code = psResponse?.Code ?? jsonResponse?.Code ?? AuthorizationResponseCode.NoMatchingRuleForComputer;

            return summaryResponse;
        }
    }
}