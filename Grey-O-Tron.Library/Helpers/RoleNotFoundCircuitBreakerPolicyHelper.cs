using System;
using GreyOTron.Library.Exceptions;
using Polly;
using Polly.CircuitBreaker;

namespace GreyOTron.Library.Helpers
{
    public class RoleNotFoundCircuitBreakerPolicyHelper
    {
        public AsyncCircuitBreakerPolicy RoleNotFoundCircuitBreakerPolicy { get; private set; }
        public RoleNotFoundCircuitBreakerPolicyHelper()
        {
            RoleNotFoundCircuitBreakerPolicy =
                Policy.Handle<RoleNotFoundException>().CircuitBreakerAsync(2, TimeSpan.FromHours(1));
        }
    }
}
