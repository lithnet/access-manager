using System;
using Microsoft.AspNetCore.Mvc;

namespace Lithnet.AccessManager.Api
{
    public interface IApiErrorResponseProvider
    {
        IActionResult GetErrorResult(Exception ex);
    }
}