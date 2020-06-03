using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using Lithnet.Laps.Web.Internal;

namespace Lithnet.Laps.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables("laps")
                .AddCommandLine(args)
                .Build();

            var host = Host.CreateDefaultBuilder(args)
                 .ConfigureAppConfiguration(builder =>
                 {
                     builder.AddJsonFile("appsecrets.json");
                     builder.AddEnvironmentVariables("laps");
                     config = builder.Build();
                 }).ConfigureWebHostDefaults(webBuilder =>
                 {
                     webBuilder.UseStartup<Startup>();
                     webBuilder.UseConfiguration(config);
                     webBuilder.UseHttpSys(config);
                 })
                 .UseNLog()
                 .UseWindowsService();

            return host;
        }
    }
}
