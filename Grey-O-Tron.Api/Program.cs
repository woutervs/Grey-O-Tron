using System;
using System.Threading.Tasks;
using Autofac.Extensions.DependencyInjection;
using GreyOTron.Library.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Environments = GreyOTron.Library.Helpers.Environments;

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
                        if (EnvironmentHelper.Is(Environments.Development))
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
