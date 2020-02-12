using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Settlement.WebApi.BackgroundServices;

namespace Settlement.WebApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging => {
                    // Adjust default logging level
                    logging.SetMinimumLevel(LogLevel.Information); 
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton<MissingSettlementService>();
                });
    }
}

/// TODO:
// 1. Implement background process that will execute long running tasks.
// 2. Built-in authentication to Azure (env variable, MSI, etc...) in order to:
//      a. read/write blobs
//      b. read/write messages