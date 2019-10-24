using System;
using System.Threading.Tasks;
using Autofac;
using Microsoft.Azure.WebJobs;

namespace GreyOTron
{
    public class Program
    {
        public static async Task Main()
        {
            using (var watcher = new WebJobsShutdownWatcher())
            {
                var container = AutofacConfiguration.Build();
                var bot = container.Resolve<Bot>();
                await bot.Start(watcher.Token);
                await bot.Stop();
                Environment.Exit(-1);
            }
        }
    }
}
