using System;
using System.Diagnostics;
using System.Reflection;
using Autofac;
using Autofac.Core;
using GreyOTron.CommandParser;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Helpers
{
    public static class AutofacConfiguration
    {
        public static IContainer Build()
        {
            var builder = new ContainerBuilder();

            builder.RegisterInstance(BootstrapConfiguration()).As<IConfigurationRoot>().SingleInstance();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AsSelf().AsImplementedInterfaces().SingleInstance()
                .Except<IConfigurationRoot>()
                .Except<ICommand>();

            return builder.Build();
        }

        private static IConfigurationRoot BootstrapConfiguration()
        {
            Trace.WriteLine("Setting up configuration");
            var builder = new ConfigurationBuilder();
            string env = Environment.GetEnvironmentVariable("Environment");
            Trace.WriteLine(env);

            if (env == "Development")
            {
                builder.AddUserSecrets<Program>();
            }
            builder.AddEnvironmentVariables();
            return builder.Build();
        }
    }
}
