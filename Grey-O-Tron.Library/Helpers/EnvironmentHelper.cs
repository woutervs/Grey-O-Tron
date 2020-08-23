using System;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Helpers
{
    public enum Environments
    {
        Development,
        Maintenance,
        Production
    }

    public class EnvironmentHelper : IEnvironmentHelper
    {
        public EnvironmentHelper()
        {
            var environmentVariable = Environment.GetEnvironmentVariable("Environment");
            if (!Enum.TryParse<Environments>(environmentVariable, true, out var environment))
            {
                throw new Exception("No environment set!");
            }
            Current = environment;
        }

        public Environments Current { get; }


        public bool Is(Environments environment)
        {
            return Current == environment;
        }
    }
}
