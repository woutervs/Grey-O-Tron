using System.Reflection;
using Autofac;
using Autofac.Extras.AttributeMetadata;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Configuration;

namespace GreyOTron.Library.Helpers
{
    public static class AutofacConfigurationHelper
    {
        public static void BuildLibrary(ref ContainerBuilder builder)
        {
            builder.RegisterModule<AttributedMetadataModule>();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(c => c.IsAssignableFrom(typeof(ICommand)))
                .AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AsSelf().AsImplementedInterfaces().SingleInstance()
                .Except<ICommand>();

            builder.RegisterType<TelemetryClient>().AsSelf();
            builder.Register(context =>
            {
                var configuration = context.Resolve<IConfiguration>();
                var telemetryConfiguration = TelemetryConfiguration.CreateDefault();
                var instrumentationKey = configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
                if (!string.IsNullOrWhiteSpace(instrumentationKey))
                {
                    telemetryConfiguration.InstrumentationKey = instrumentationKey;
                }
                //Could add processors/sinks here...
                return telemetryConfiguration;
            }).SingleInstance();
        }
    }
}
