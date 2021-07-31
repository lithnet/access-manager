using System;
using System.Net.Http;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Shared;

namespace Lithnet.AccessManager.Agent.Providers
{
    public interface IAmsApiHttpClient
    {
        Task CheckInAsync(AgentCheckIn data);

        Task RollbackPasswordUpdateAsync(string passwordId);
        
        Task<PasswordUpdateResponse> UpdatePasswordAsync(PasswordUpdateRequest request);
        
        Task<PasswordGetResponse> GetPasswordChangeRequiredAsync();
       
        Task RegisterSecondaryCredentialsAsync(ClientAssertion assertion);
        
        Task<RegistrationResponse> RegisterAgentAsync(ClientAssertion assertion);
        
        Task<TokenResponse> RequestAccessTokenX509Async(ClientAssertion assertion);
        
        Task<TokenResponse> RequestAccessTokenIwaAsync();
        
        Uri BaseAddress { get; }
        string BuildUrl(string path);
    }
}