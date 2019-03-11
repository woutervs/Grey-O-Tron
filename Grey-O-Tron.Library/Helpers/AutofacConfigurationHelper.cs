using System.Reflection;
using Autofac;
using Autofac.Extras.AttributeMetadata;
using Microsoft.ApplicationInsights;

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
        }
    }
}
