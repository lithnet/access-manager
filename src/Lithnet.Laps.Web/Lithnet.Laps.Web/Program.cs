using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using Lithnet.Laps.Web.Internal;
using System.Reflection;

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
                     builder.AddJsonFile("appsecrets.json", true);
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
