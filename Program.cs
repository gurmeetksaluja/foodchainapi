using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Filters;
using Serilog.Formatting.Compact;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoodChainBrandsAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
    //   .MinimumLevel.Information()
    //.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    // Filter out ASP.NET Core infrastructre logs that are Information and below
    //   .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)

    .Enrich.FromLogContext()
    .Filter.ByExcluding(c => c.Properties.Any(p => p.Value.ToString().Contains("swagger")))
    .Filter.ByExcluding(Matching.FromSource("Microsoft.AspNetCore.Hosting.Internal.WebHost"))
    .WriteTo.Console()
    .WriteTo.File(new CompactJsonFormatter(), "Logs/Logs/logs.json", rollingInterval: RollingInterval.Day)
       .CreateLogger();

            // Wrap creating and running the host in a try-catch block
            try
            {
                Log.Information("Starting host");
                CreateHostBuilder(args).Build().Run();
                //  return 0;
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Host terminated unexpectedly");
                //  return 1;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
          Host.CreateDefaultBuilder(args)
            .UseSerilog() // <- Add this line
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
            });
    }
}
