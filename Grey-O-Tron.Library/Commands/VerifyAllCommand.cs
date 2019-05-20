using System;
using System.Collections.Generic;
using System.Text;
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

namespace GreyOTron.Library.Commands
{
    [Command("gw2-verify-all", CommandDescription = "Verifies all users on the server.", CommandOptions = CommandOptions.DiscordServer | CommandOptions.RequiresAdmin)]
    public class VerifyAllCommand : ICommand
    {
        private readonly IConfiguration configuration;
        private readonly KeyRepository keyRepository;
        private readonly Gw2Api gw2Api;
        private readonly VerifyUser verifyUser;
        private readonly TelemetryClient log;

        public VerifyAllCommand(IConfiguration configuration, KeyRepository keyRepository, Gw2Api gw2Api, VerifyUser verifyUser, TelemetryClient log)
        {
            this.configuration = configuration;
            this.keyRepository = keyRepository;
            this.gw2Api = gw2Api;
            this.verifyUser = verifyUser;
            this.log = log;
        }
        public async Task Execute(SocketMessage message, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            if (message.Author is SocketGuildUser guildUser)
            {
                await guildUser.SendMessageAsync(
                    "Command is temporarily disabled.");
                //if (guildUser.GuildPermissions.Administrator || guildUser.Id == ulong.Parse(configuration["OwnerId"]))
                //{
                //    SocketGuildUser currentUser = null;
                //    try
                //    {
                //        foreach (var socketGuildUser in guildUser.Guild.Users)
                //        {
                //            currentUser = socketGuildUser;
                //            var discordClientWithKey = await keyRepository.Get("Gw2", currentUser.Id.ToString());
                //            if (discordClientWithKey == null) continue;
                //            var acInfo = gw2Api.GetInformationForUserByKey(discordClientWithKey.Key);
                //            if (acInfo != null)
                //            {
                //                await verifyUser.Verify(acInfo, currentUser,currentUser, true);
                //            }
                //            else
                //            {
                //                await message.Author.SendMessageAsync("The GW2Api is unavailable at this time, please try again later.");
                //                break;
                //            }
                //        }
                //    }
                //    catch (Exception e)
                //    {
                //        if (currentUser != null)
                //        {
                //            log.TrackTrace(JsonConvert.SerializeObject(currentUser, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
                //        }
                //        log.TrackException(e);
                //    }
                //}
                //else
                //{
                //    await guildUser.SendMessageAsync(
                //        "You must have administrative permissions to perform the verify-all command.");
                //}
            }
            else
            {
                await message.Author.SendMessageAsync("This command must be used from within the server to which you want to apply it.");
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
