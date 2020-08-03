using System;
using System.Collections.Generic;
using System.Security.AccessControl;
using System.Security.Principal;
using Lithnet.AccessManager.Server.Authorization;
using Lithnet.AccessManager.Server.Configuration;
using Lithnet.Security.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Lithnet.AccessManager.Server.Test
{
    public class AuthorizationContextProviderTests
    {
        private IDirectory directory;

        private ILogger<AuthorizationContextProvider> logger;

        [SetUp()]
        public void TestInitialize()
        {
            directory = new ActiveDirectory();
            logger = Global.LogFactory.CreateLogger<AuthorizationContextProvider>();
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1")]
        public void TestFailoverToNextHost(string username, string computerName)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);
            string dc = this.directory.GetDomainControllerForDomain(dnsDomain);

            var options = new BuiltInProviderOptions
            {
                AuthorizationServerMapping = new List<AuthorizationServerMapping>
                {
                     new AuthorizationServerMapping
                     {
                          Domain = dnsDomain,
                          DisableLocalFallback = true,
                          Servers = new List<AuthorizationServer>
                          {
                                new AuthorizationServer
                                {
                                     Name = "madeup.local",
                                     Type = AuthorizationServerType.Default
                                },
                                new AuthorizationServer
                                {
                                    Name = dc,
                                    Type = AuthorizationServerType.Default
                                }
                          }
                     }
                }
            };

            var authorizationContextProvider = new AuthorizationContextProvider(this.SetupOptions(options), directory, logger);
            var c = authorizationContextProvider.GetAuthorizationContext(user, computer);

            c = authorizationContextProvider.GetAuthorizationContext(user, computer);

            Assert.AreEqual(c.Server, dc);
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1")]
        public void ExceptionThrownWhenHostListRecycles(string username, string computerName)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);

            var options = new BuiltInProviderOptions
            {
                AuthorizationServerMapping = new List<AuthorizationServerMapping>
                {
                    new AuthorizationServerMapping
                    {
                        Domain = dnsDomain,
                        DisableLocalFallback = true,
                        Servers = new List<AuthorizationServer>
                        {
                            new AuthorizationServer
                            {
                                Name = "madeup.local",
                                Type = AuthorizationServerType.Default
                            },
                            new AuthorizationServer
                            {
                                Name = "madeup2.local",
                                Type = AuthorizationServerType.Default
                            },
                            new AuthorizationServer
                            {
                                Name = "madeup3.local",
                                Type = AuthorizationServerType.Default
                            },
                        }
                    }
                }
            };

            var authorizationContextProvider = new AuthorizationContextProvider(this.SetupOptions(options), directory, logger);
            Assert.Throws<AuthorizationContextException>(() => authorizationContextProvider.GetAuthorizationContext(user, computer));
        }

        [TestCase("IDMDEV1\\user1", "IDMDEV1\\PC1")]
        public void TestFailbackToLocalHost(string username, string computerName)
        {
            IUser user = directory.GetUser(username);
            IComputer computer = directory.GetComputer(computerName);
            string dnsDomain = this.directory.GetDomainNameDnsFromSid(user.Sid);

            var options = new BuiltInProviderOptions
            {
                AuthorizationServerMapping = new List<AuthorizationServerMapping>
                {
                    new AuthorizationServerMapping
                    {
                        Domain = dnsDomain,
                        DisableLocalFallback = false,
                        Servers = new List<AuthorizationServer>
                        {
                            new AuthorizationServer
                            {
                                Name = "madeup.local",
                                Type = AuthorizationServerType.Default
                            }
                        }
                    }
                }
            };

            var authorizationContextProvider = new AuthorizationContextProvider(this.SetupOptions(options), directory, logger);
            var c = authorizationContextProvider.GetAuthorizationContext(user, computer);

            Assert.AreEqual(c.Server, null);
        }

        private IOptionsSnapshot<BuiltInProviderOptions> SetupOptions(BuiltInProviderOptions options)
        {
            Mock<IOptionsSnapshot<BuiltInProviderOptions>> optionsSnapshot = new Mock<IOptionsSnapshot<BuiltInProviderOptions>>();
            optionsSnapshot.SetupGet(t => t.Value).Returns((BuiltInProviderOptions)options);
            return optionsSnapshot.Object;
        }
    }
}