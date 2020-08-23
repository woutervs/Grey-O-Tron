using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac.Features.Metadata;
using Discord;
using Discord.WebSocket;
using GreyOTron.Library.Attributes;
using GreyOTron.Library.Interfaces;
using GreyOTron.Resources;

namespace GreyOTron.Library.Extensions
{
    public static class MetaCommandExtensions
    {
        public static async Task Execute(this Meta<ICommand> commandWithMeta, DiscordSocketClient client, IMessage message, CancellationToken cancellationToken)
        {
            var canExecute = false;
            commandWithMeta.Value.Client = client;
            //Manual command eg. null, 
            if (!commandWithMeta.Metadata.Any())
            {
                canExecute = true;
            }
            else
            {

                if (commandWithMeta.Metadata.ContainsKey(nameof(CommandAttribute.CommandOptions)))
                {
                    var commandName = commandWithMeta.Metadata[nameof(CommandAttribute.CommandName)]?.ToString();
                    if (commandWithMeta.Metadata[nameof(CommandAttribute.CommandOptions)] is CommandOptions commandOptions)
                    {
                        var requiresOwner = commandOptions.HasFlag(CommandOptions.RequiresOwner);
                        var requiresAdmin = commandOptions.HasFlag(CommandOptions.RequiresAdmin);
                        var discordServer = commandOptions.HasFlag(CommandOptions.DiscordServer);
                        var directMessage = commandOptions.HasFlag(CommandOptions.DirectMessage);

                        var isGuildUser = message.Author is IGuildUser;

                        if (discordServer && !directMessage && !isGuildUser)
                        {
                            await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.ServerOnlyCommand), commandName);
                        }
                        else if (requiresOwner && !message.Author.IsOwner())
                        {
                            await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.Unauthorized));
                        }
                        else if ((requiresAdmin || requiresOwner) && !message.Author.IsAdminOrOwner())
                        {
                            await message.Author.InternalSendMessageAsync(nameof(GreyOTronResources.AdministrativePermissionsOnly), commandName);
                        }
                        else
                        {
                            canExecute = true;
                        }
                    }
                }
            }

            if (canExecute)
            {
                await commandWithMeta.Value.Execute(message, cancellationToken);
            }
            if (!(message.Channel is SocketDMChannel))
            {
                await message.DeleteAsync();
            }
        }


    }
}
