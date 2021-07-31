using System;
using Lithnet.AccessManager.Api.Shared;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Agent.Providers
{
    public class AmsApiHttpClient : IAmsApiHttpClient
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly JsonSerializerOptions jsonOptions;
        private Uri baseAddress;

        public AmsApiHttpClient(IHttpClientFactory httpClientFactory, JsonSerializerOptions jsonOptions)
        {
            this.httpClientFactory = httpClientFactory;
            this.jsonOptions = jsonOptions;
        }

        public Uri BaseAddress
        {
            get
            {
                if (this.baseAddress == null)
                {
                    this.baseAddress = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer).BaseAddress;
                }

                return this.baseAddress;
            }
        }

        public string BuildUrl(string path)
        {
            return new Uri(this.BaseAddress, path).ToString();
        }

        public async Task CheckInAsync(AgentCheckIn data)
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer))
            {
                using (var httpResponseMessage = await client.PostAsync($"agent/checkin", data.AsJsonStringContent()))
                {
                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);
                }
            }
        }

        public async Task RollbackPasswordUpdateAsync(string passwordId)
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer))
            {
                using (var httpResponseMessage = await client.DeleteAsync($"agent/password/{passwordId}"))
                {
                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);
                }
            }
        }

        public async Task<PasswordUpdateResponse> UpdatePasswordAsync(PasswordUpdateRequest request)
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer))
            {
                using (var httpResponseMessage = await client.PostAsync($"agent/password", request.AsJsonStringContent()))
                {

                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);

                    var response = JsonSerializer.Deserialize<PasswordUpdateResponse>(responseString, this.jsonOptions);

                    if (response == null)
                    {
                        throw new UnexpectedResponseException("The server returned an unexpected response");
                    }

                    return response;
                }
            }
        }

        public async Task<PasswordGetResponse> GetPasswordChangeRequiredAsync()
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer))
            {
                using (var httpResponseMessage = await client.GetAsync($"agent/password"))
                {
                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();

                    if (httpResponseMessage.IsSuccessStatusCode)
                    {
                        if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.NoContent)
                        {
                            return null;
                        }

                        if (httpResponseMessage.StatusCode == System.Net.HttpStatusCode.ResetContent)
                        {
                            return JsonSerializer.Deserialize<PasswordGetResponse>(responseString, this.jsonOptions);
                        }
                    }

                    httpResponseMessage.EnsureSuccessStatusCode(responseString);
                    throw new UnexpectedResponseException($"The API returned an unexpected status code of {httpResponseMessage.StatusCode}");
                }
            }
        }

        public async Task RegisterSecondaryCredentialsAsync(ClientAssertion assertion)
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthBearer))
            {
                using (var httpResponseMessage = await client.PostAsync($"agent/register/credential", assertion.AsJsonStringContent()))
                {
                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);
                }
            }
        }

        public async Task<RegistrationResponse> RegisterAgentAsync(ClientAssertion assertion)
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous))
            {
                using (var httpResponseMessage = await client.PostAsync($"agent/register", assertion.AsJsonStringContent()))
                {
                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);

                    return JsonSerializer.Deserialize<RegistrationResponse>(responseString, this.jsonOptions) ?? throw new UnexpectedResponseException("The response body returned by the server was invalid");
                }
            }
        }

        public async Task<TokenResponse> RequestAccessTokenX509Async(ClientAssertion assertion)
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthAnonymous))
            {
                using (var httpResponseMessage = await client.PostAsync("auth/x509", assertion.AsJsonStringContent()))
                {
                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);
                    return JsonSerializer.Deserialize<TokenResponse>(responseString);
                }
            }
        }

        public async Task<TokenResponse> RequestAccessTokenIwaAsync()
        {
            using (var client = this.httpClientFactory.CreateClient(Constants.HttpClientAuthIwa))
            {
                using (var httpResponseMessage = await client.GetAsync("auth/iwa"))
                {
                    var responseString = await httpResponseMessage.Content.ReadAsStringAsync();
                    httpResponseMessage.EnsureSuccessStatusCode(responseString);

                    return JsonSerializer.Deserialize<TokenResponse>(responseString);
                }
            }
        }
    }
}
