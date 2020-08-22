using System;
using Microsoft.ApplicationInsights;

namespace GreyOTron.Library.Helpers
{
    public enum Environments
    {
        Development,
        Maintenance,
        Production
    }

    public static class EnvironmentHelper
    {
        static EnvironmentHelper()
        {
            var environmentVariable = Environment.GetEnvironmentVariable("Environment");
            if (!Enum.TryParse<Environments>(environmentVariable, true, out var environment))
            {
                throw new Exception("No environment set!");
            }
            Current = environment;
        }

        public static Environments Current { get; }


        public static bool Is(Environments environment)
        {
            return Current == environment;
        }
    }
}
