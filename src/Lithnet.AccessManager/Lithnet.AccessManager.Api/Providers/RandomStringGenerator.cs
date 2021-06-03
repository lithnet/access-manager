using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Lithnet.AccessManager.Server;
using System.Collections.Concurrent;

namespace Lithnet.AccessManager.Api.Providers
{
    public class RandomStringGenerator 
    {
        private readonly RNGCryptoServiceProvider csp;

        public RandomStringGenerator(RNGCryptoServiceProvider csp)
        {
            this.csp = csp;
        }

        public string Generate(int binaryLength)
        {
            return Convert.ToBase64String(this.GetRandomBytes(binaryLength));
        }

        public string Generate()
        {
            return this.Generate(32);
        }

        private byte[] GetRandomBytes(int size)
        {
            byte[] buffer = new byte[size];
            this.csp.GetBytes(buffer);
            return buffer;
        }
    }
}
