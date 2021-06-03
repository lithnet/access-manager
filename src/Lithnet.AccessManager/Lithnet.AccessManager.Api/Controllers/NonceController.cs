using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lithnet.AccessManager.Api.Providers;

namespace Lithnet.AccessManager.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NonceController : Controller
    {
        private readonly ILogger<NonceController> logger;

        private readonly IReplayNonceProvider nonceProvider;

        public NonceController(ILogger<NonceController> logger, IReplayNonceProvider nonceProvider)
        {
            this.logger = logger;
            this.nonceProvider = nonceProvider;
        }

        [HttpGet]
        public IActionResult Get()
        {
            this.Response.Headers.Add("Replay-Nonce", this.nonceProvider.GenerateNonce());
            return this.Ok();
        }

        [HttpGet("{nonce}")]
        public IActionResult Get(string nonce)
        {
            this.Response.Headers.Add("Replay-Nonce", this.nonceProvider.GenerateNonce());

            if (this.nonceProvider.ConsumeNonce(nonce))
            {
                return this.Ok();
            }

            return this.BadRequest();
        }
    }
}
