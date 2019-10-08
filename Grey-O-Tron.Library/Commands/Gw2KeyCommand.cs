using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.TableStorage;
using Microsoft.ApplicationInsights;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly.CircuitBreaker;

namespace GreyOTron.Library.Commands
{
    [Command("gw2-key", CommandDescription = "Stores Guild Wars 2 key in the database.", CommandArguments = "{key}", CommandOptions = CommandOptions.DirectMessage | CommandOptions.DiscordServer)]
    public class Gw2KeyCommand : ICommand
    {
        private readonly Gw2Api gw2Api;
        private readonly KeyRepository gw2KeyRepository;
        private readonly IConfiguration configuration;
        private readonly VerifyUser verifyUser;
        private readonly TelemetryClient log;

        public Gw2KeyCommand(Gw2Api gw2Api, KeyRepository gw2KeyRepository, IConfiguration configuration, VerifyUser verifyUser, TelemetryClient log)
        {
            this.gw2Api = gw2Api;
            this.gw2KeyRepository = gw2KeyRepository;
            this.configuration = configuration;
            this.verifyUser = verifyUser;
            this.log = log;
        }

        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;
            var key = Arguments;
            if (string.IsNullOrWhiteSpace(key))
            {
                await message.Author.SendMessageAsync("This key seems empty to me, please try again.");
            }
            else
            {
                AccountInfo acInfo;
                try
                {
                    acInfo = gw2Api.GetInformationForUserByKey(key);
                }
                catch (BrokenCircuitException)
                {
                    await message.Author.SendMessageAsync("The GW2 api can't handle this request at the time, please try again a bit later.");
                    throw;
                }
                log.TrackTrace(message.Content, new Dictionary<string, string> { { "DiscordUser", $"{message.Author.Username}#{message.Author.Discriminator}" }, { "AccountInfo", JsonConvert.SerializeObject(acInfo, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }) } });
                if (acInfo?.TokenInfo?.Name == $"{message.Author.Username}#{message.Author.Discriminator}")
                {
                    await gw2KeyRepository.Set(new DiscordClientWithKey("Gw2", message.Author.Id.ToString(),
                        $"{message.Author.Username}#{message.Author.Discriminator}", key));

                    if (message.Author is SocketGuildUser guildUser)
                    {
                        await verifyUser.Verify(acInfo, guildUser, guildUser);
                    }
                    else
                    {
                        foreach (var guild in message.Author.MutualGuilds)
                        {
                            guildUser = guild.GetUser(message.Author.Id);
                            await verifyUser.Verify(acInfo, guildUser, guildUser);
                        }
                        await message.Author.SendMessageAsync($"Your key has been stored, and we verified you on \n {message.Author.MutualGuilds.Aggregate("", (x, y) => $"{x}{y.Name}\n")}");
                    }
                }
                else
                {
                    await message.Author.SendMessageAsync($"Please make sure your GW2 application key's name is the same as your discord username: {message.Author.Username}#{message.Author.Discriminator}");
                    await message.Author.SendMessageAsync("You can view, create and edit your GW2 application key's on https://account.arena.net/applications");
                }
            }

            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }

        public string Arguments { get; set; }
        public DiscordSocketClient Client { get; set; }
    }
}
