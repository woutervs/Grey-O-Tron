using System;
using System.Diagnostics;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureServices(s => s.AddAutofac())
                .ConfigureAppConfiguration(builder =>
                {
                    string env = Environment.GetEnvironmentVariable("Environment");
                    if (env == "Development")
                    {
                        builder.AddUserSecrets<Program>();
                    }
                    builder.AddJsonFile("app.json");
                    builder.AddEnvironmentVariables();
                })
                .UseStartup<Startup>();
    }
}
