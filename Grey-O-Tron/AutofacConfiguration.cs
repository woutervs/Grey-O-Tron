using System;
using System.Diagnostics;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Extras.AttributeMetadata;
using GreyOTron.Library.Commands;
using GreyOTron.Library.Helpers;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public static class AutofacConfiguration
    {
        public static IContainer Build()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<AttributedMetadataModule>();

            builder.RegisterInstance(BootstrapConfiguration()).As<IConfigurationRoot>().SingleInstance();

            AutofacConfigurationHelper.BuildLibrary(ref builder);

            builder.RegisterType<CommandProcessor>().AsSelf().WithParameter(
                new ResolvedParameter((info, context) => info.ParameterType == typeof(string) && info.Name == "prefix",
                    (info, context) => context.Resolve<IConfigurationRoot>()["command-prefix"]
             ));

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
            builder.AddJsonFile("app.json");
            builder.AddEnvironmentVariables();
            return builder.Build();
        }
    }
}
