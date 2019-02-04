using System.Reflection;
using Autofac;
using GreyOTron.Library.Commands;

namespace GreyOTron.Library.Helpers
{
    public static class AutofacConfigurationHelper
    {
        public static void BuildLibrary(ref ContainerBuilder builder)
        {
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).Where(c => c.IsAssignableFrom(typeof(ICommand)))
                .AsImplementedInterfaces().InstancePerDependency();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly()).AsSelf().AsImplementedInterfaces().SingleInstance()
                .Except<ICommand>();
        }
    }
}
