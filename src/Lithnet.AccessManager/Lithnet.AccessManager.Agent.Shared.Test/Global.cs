using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using NUnit.Framework;


namespace Lithnet.AccessManager.Agent.Providers.Test
{
    [SetUpFixture]
    public class Global
    {
        public static ILoggerFactory LogFactory;

        public static X509Certificate2 TestCertificate;


        [OneTimeSetUp]
        [Obsolete]
        public void RunBeforeAnyTests()
        {
            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile") { FileName = "unit-test.log" };
            var logconsole = new NLog.Targets.ConsoleTarget("logconsole");
            var logdebugger = new NLog.Targets.DebuggerTarget("logdebugger");

            // Rules for mapping loggers to targets            
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logconsole);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logfile);
            config.AddRule(NLog.LogLevel.Trace, NLog.LogLevel.Fatal, logdebugger);

            // Apply config           
            NLog.LogManager.Configuration = config;

            var serviceProvider = new ServiceCollection()
                .AddLogging(logger =>
                {
                    logger.SetMinimumLevel(LogLevel.Trace);
                })
                .BuildServiceProvider();

            LogFactory = serviceProvider.GetService<ILoggerFactory>();
            LogFactory.AddNLog();

            TestCertificate = new X509Certificate2("TestData\\cert.cer");
        }
    }
}
