using System;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace GreyOTron.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) => Host.CreateDefaultBuilder(args)
            .UseServiceProviderFactory(new AutofacServiceProviderFactory())
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder
                    .ConfigureAppConfiguration(configurationBuilder =>
                    {
                        var env = Environment.GetEnvironmentVariable("Environment");
                        if (env == "Development")
                        {
                            configurationBuilder.AddUserSecrets<Program>();
                        }
                        configurationBuilder.AddJsonFile("app.json");
                        configurationBuilder.AddEnvironmentVariables();
                    })
                    .UseStartup<Startup>();
            });
    }
}
