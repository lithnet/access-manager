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
using C = Lithnet.AccessManager.Test.TestEnvironmentConstants;

namespace Lithnet.AccessManager.Server.Test
{
    public class AuthorizationContextProviderTests
    {
        private IDirectory directory;

        private IDiscoveryServices discoveryServices;

        private ILogger<AuthorizationContextProvider> logger;

        [SetUp()]
        public void TestInitialize()
        {
            this.discoveryServices = new DiscoveryServices(Mock.Of<ILogger<DiscoveryServices>>());
            directory = new ActiveDirectory(discoveryServices);
            logger = Global.LogFactory.CreateLogger<AuthorizationContextProvider>();
        }

        [TestCase(C.DEV_User1, C.DEV_PC1)]
        public void TestFailoverToNextHost(string username, string computerName)
        {
            IUser user = directory.GetUser(username);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);
            string dnsDomain = this.discoveryServices.GetDomainNameDns(user.Sid);
            string dc = this.discoveryServices.GetDomainController(dnsDomain);

            var options = new AuthorizationOptions
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

            var authorizationContextProvider = new AuthorizationContextProvider(this.SetupOptions(options), logger, discoveryServices);
            var c = authorizationContextProvider.GetAuthorizationContext(user, computer.Sid);

            c = authorizationContextProvider.GetAuthorizationContext(user, computer.Sid);

            Assert.AreEqual(c.Server, dc);
        }

        [TestCase(C.DEV_User1, C.DEV_PC1)]
        public void ExceptionThrownWhenHostListRecycles(string username, string computerName)
        {
            IUser user = directory.GetUser(username);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);
            string dnsDomain = this.discoveryServices.GetDomainNameDns(user.Sid);

            var options = new AuthorizationOptions
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

            var authorizationContextProvider = new AuthorizationContextProvider(this.SetupOptions(options), logger, discoveryServices);
            Assert.Throws<AuthorizationContextException>(() => authorizationContextProvider.GetAuthorizationContext(user, computer.Sid));
        }

        [TestCase(C.DEV_User1, C.DEV_PC1)]
        public void TestFailbackToLocalHost(string username, string computerName)
        {
            IUser user = directory.GetUser(username);
            IActiveDirectoryComputer computer = directory.GetComputer(computerName);
            string dnsDomain = this.discoveryServices.GetDomainNameDns(user.Sid);

            var options = new AuthorizationOptions
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

            var authorizationContextProvider = new AuthorizationContextProvider(this.SetupOptions(options), logger, discoveryServices);
            var c = authorizationContextProvider.GetAuthorizationContext(user, computer.Sid);

            Assert.AreEqual(c.Server, null);
        }

        private IOptionsSnapshot<AuthorizationOptions> SetupOptions(AuthorizationOptions options)
        {
            Mock<IOptionsSnapshot<AuthorizationOptions>> optionsSnapshot = new Mock<IOptionsSnapshot<AuthorizationOptions>>();
            optionsSnapshot.SetupGet(t => t.Value).Returns((AuthorizationOptions)options);
            return optionsSnapshot.Object;
        }
    }
}