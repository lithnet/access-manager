using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lithnet.AccessManager.Api.Models
{
    public class ApiError
    {
        public string Code { get; set; }

        public string Message { get; set; }

        public object Details { get; set; }

        public ApiError()
        {
        }

        public ApiError(string code, string message)
        : this(code, message, null)
        {
        }

        public ApiError(string code, string message, object details)
        {
            this.Code = code;
            this.Message = message;
            this.Details = details;
        }
    }
}
