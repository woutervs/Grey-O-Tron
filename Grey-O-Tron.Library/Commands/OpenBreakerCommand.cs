using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using Polly.CircuitBreaker;

namespace GreyOTron.Library.Commands
{
    [Command("open-breaker", CommandDescription = "Opens the breaker again.", CommandOptions = CommandOptions.RequiresOwner)]
    public class OpenBreakerCommand : ICommand
    {
        private readonly RoleNotFoundCircuitBreakerPolicyHelper roleNotFoundCircuitBreakerPolicyHelper;

        public OpenBreakerCommand(RoleNotFoundCircuitBreakerPolicyHelper roleNotFoundCircuitBreakerPolicyHelper)
        {
            this.roleNotFoundCircuitBreakerPolicyHelper = roleNotFoundCircuitBreakerPolicyHelper;
        }
        public async Task Execute(IMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var state = roleNotFoundCircuitBreakerPolicyHelper.RoleNotFoundCircuitBreakerPolicy.CircuitState
                .ToString();
            if (roleNotFoundCircuitBreakerPolicyHelper.RoleNotFoundCircuitBreakerPolicy.CircuitState !=
                CircuitState.Closed)
            {
                roleNotFoundCircuitBreakerPolicyHelper.RoleNotFoundCircuitBreakerPolicy.Reset();

            }

            await message.Author.InternalSendMessageAsync(
                $"Changed breaker from {state} to {roleNotFoundCircuitBreakerPolicyHelper.RoleNotFoundCircuitBreakerPolicy.CircuitState}");
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
