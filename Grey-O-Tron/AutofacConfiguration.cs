using System.Diagnostics;
using Autofac;
using Autofac.Core;
using GreyOTron.Library.Helpers;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Configuration;

namespace GreyOTron
{
    public static class AutofacConfiguration
    {
        public static IContainer Build()
        {
            var environmentHelper = new EnvironmentHelper();

            var builder = new ContainerBuilder();

            builder.RegisterInstance(environmentHelper).AsImplementedInterfaces().SingleInstance();

            builder.RegisterInstance(BootstrapConfiguration(environmentHelper)).As<IConfiguration>().SingleInstance();

            AutofacConfigurationHelper.BuildLibrary(ref builder);

            builder.RegisterType<CommandProcessor>().AsSelf().WithParameter(
                new ResolvedParameter((info, context) => info.ParameterType == typeof(string) && info.Name == "prefix",
                    (info, context) => context.Resolve<IConfiguration>()["CommandPrefix"]
             ));

            builder.RegisterType<AzureServiceTokenProvider>().SingleInstance();
            builder.RegisterType<Bot>().AsSelf().SingleInstance();
            
            if (environmentHelper.Is(Environments.Development))
            {
                builder.RegisterType<SqlLocalDbConfiguration>().AsImplementedInterfaces().SingleInstance();
            }
            else
            {
                builder.RegisterType<SqlDbConfiguration>().AsImplementedInterfaces().SingleInstance();
            }
            

            return builder.Build();
        }

        private static IConfiguration BootstrapConfiguration(EnvironmentHelper environmentHelper)
        {
            Trace.WriteLine("Setting up configuration");
            var builder = new ConfigurationBuilder();
            Trace.WriteLine(environmentHelper.Current);

            if (environmentHelper.Is(Environments.Development) || environmentHelper.Is(Environments.Maintenance))
            {
                builder.AddUserSecrets<Program>();
            }
            builder.AddJsonFile("app.json");
            builder.AddJsonFile($"app.{environmentHelper.Current.ToString().ToLowerInvariant()}.json", true);
            builder.AddEnvironmentVariables();
            return builder.Build();
        }
    }
}
