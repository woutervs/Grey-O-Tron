using System;
using System.Diagnostics;
using System.Reflection;
using Autofac;
using Autofac.Core;
using Autofac.Extras.AttributeMetadata;
using GreyOTron.Commands;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Helpers
{
    public static class AutofacConfiguration
    {
        public static IContainer Build()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<AttributedMetadataModule>();

            builder.RegisterInstance(BootstrapConfiguration()).As<IConfigurationRoot>().SingleInstance();


            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(c => c.IsAssignableFrom(typeof(ICommand)))
                .AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AsSelf().AsImplementedInterfaces().SingleInstance()
                .Except<IConfigurationRoot>()
                .Except<ICommand>();

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
