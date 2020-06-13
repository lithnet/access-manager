using Lithnet.Laps.Web.ActiveDirectory;
using Lithnet.Laps.Web.AppSettings;
using Lithnet.Laps.Web.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation.Language;

namespace Lithnet.Laps.Web.Authorization
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

        public JitAuthorizationResponse GetJitAuthorizationResponse(IUser user, IComputer computer)
        {
            return this.GetAuthorizationResponse(user, computer, this.enabledProviders.Select<IAuthorizationService, Func<IUser, IComputer, JitAuthorizationResponse>>(t => t.GetJitAuthorizationResponse).ToArray());
        }

        public LapsAuthorizationResponse GetLapsAuthorizationResponse(IUser user, IComputer computer)
        {
            return this.GetAuthorizationResponse(user, computer, this.enabledProviders.Select<IAuthorizationService, Func<IUser, IComputer, LapsAuthorizationResponse>>(t => t.GetLapsAuthorizationResponse).ToArray());
        }

        public T GetAuthorizationResponse<T>(IUser user, IComputer computer, params Func<IUser, IComputer, T>[] providers) where T : AuthorizationResponse, new()
        {
            T response = null;
            T summaryResponse = new T();

            foreach (var provider in providers)
            {
                response = provider(user, computer);

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