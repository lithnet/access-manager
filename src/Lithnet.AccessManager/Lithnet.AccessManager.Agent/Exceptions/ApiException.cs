using Lithnet.AccessManager.Api.Shared;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.Serialization;
using System.Text;

namespace Lithnet.AccessManager.Agent
{
    [Serializable]
    public class ApiException : HttpRequestException
    {
        public ApiException()
        {
        }

        public ApiException(ApiError error, HttpResponseMessage message)
            : base(string.Format($"The API call failed with HTTP status {message.StatusCode}:{message.ReasonPhrase}. The API returned error code '{error.Code}': {error.Message}\r\n{error.Details}"))
        {
        }

        public ApiException(string message)
            : base(message)
        {
        }

        public ApiException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
