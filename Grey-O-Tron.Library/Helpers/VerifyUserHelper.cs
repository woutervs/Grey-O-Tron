using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.Extensions;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using GreyOTron.Resources;
using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;

namespace GreyOTron.Library.Helpers
{
    public class VerifyUserHelper
    {
        private readonly IGw2DiscordServerRepository gw2DiscordServerRepository;
        private readonly CacheHelper cache;
        private readonly IConfiguration configuration;
        private readonly RoleNotFoundCircuitBreakerPolicyHelper roleNotFoundCircuitBreakerPolicyHelper;
        private readonly AsyncRetryPolicy roleNotFoundExceptionRetryPolicy;

        public VerifyUserHelper(IGw2DiscordServerRepository gw2DiscordServerRepository, CacheHelper cache, IConfiguration configuration, RoleNotFoundCircuitBreakerPolicyHelper roleNotFoundCircuitBreakerPolicyHelper)
        {
            this.gw2DiscordServerRepository = gw2DiscordServerRepository;
            this.cache = cache;
            this.configuration = configuration;
            this.roleNotFoundCircuitBreakerPolicyHelper = roleNotFoundCircuitBreakerPolicyHelper;
            roleNotFoundExceptionRetryPolicy = Policy.Handle<RoleNotFoundException>()
                .WaitAndRetryAsync(1, x => TimeSpan.FromSeconds(x * 30));
        }

        public async Task Execute(AccountInfo gw2AccountInfo, SocketGuildUser guildUser, SocketGuildUser contextUser, bool bypassMessages = false)
        {
            var contextUserIsNotGuildUser = guildUser.Id != contextUser.Id;

            var gw2DiscordServer = await gw2DiscordServerRepository.Get(guildUser.Guild.Id);

            var worlds = (gw2DiscordServer?.Worlds ?? new List<Gw2WorldDto>()).Select(x => x.Name).ToList();

            if (gw2DiscordServer?.MainWorld != null && !worlds.Any(y=>y.Equals(gw2DiscordServer.MainWorld.Name, StringComparison.InvariantCultureIgnoreCase)))
            {
                worlds.Add(gw2DiscordServer.MainWorld.Name);
            }

            var userOwnedRolesMatchingWorlds = guildUser.Roles.Where(x => worlds.Any(y => y.Equals(x.Name, StringComparison.InvariantCultureIgnoreCase)) || x.Name.Equals(configuration["LinkedServerRole"], StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (gw2AccountInfo.TokenInfo.Name != $"{guildUser.Username}#{guildUser.Discriminator}")
            {
                if (contextUserIsNotGuildUser)
                {
                    await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.UserChangedDiscordUsername),
                        guildUser.Username,
                        gw2AccountInfo.TokenInfo.Name,
                        guildUser.Username,
                        guildUser.Discriminator,
                        nameof(GreyOTronResources.FindYourGW2ApplicationKey));
                }
                else
                {
                    await contextUser.InternalSendMessageAsync(
                        nameof(GreyOTronResources.YouChangedDiscordUsername),
                        gw2AccountInfo.TokenInfo.Name, guildUser.Username, guildUser.Discriminator,
                        nameof(GreyOTronResources.FindYourGW2ApplicationKey));
                }
            }
            else
            {
                if (gw2AccountInfo.WorldInfo == null)
                {
                    if (!bypassMessages)
                    {
                        if (contextUserIsNotGuildUser)
                        {
                            await contextUser.InternalSendMessageAsync(
                                nameof(GreyOTronResources.ApiFailedToReturnWorldsForUser), guildUser.Username);
                        }
                        else
                        {
                            await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources
                                .ApiFailedToReturnWorldsForYou));
                        }
                    }
                }
                else
                {
                    string role = null;
                    if (worlds.Any(y=>y.Equals(gw2AccountInfo.WorldInfo.Name, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        role = gw2AccountInfo.WorldInfo.Name;

                    }
                    else if (gw2AccountInfo.WorldInfo.LinkedWorlds.Any(x => string.Equals(x.Name, gw2DiscordServer?.MainWorld.Name, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        role = configuration["LinkedServerRole"];

                    }
                    if (role != null)
                    {
                        async Task catchRoleNotFoundOrBreakerAndNotifyUser(Func<Task<PolicyResult>> action)
                        {
                            var p = await action();
                            if (p.Outcome == OutcomeType.Failure)
                            {
                                if (!bypassMessages)
                                {
                                    if (contextUserIsNotGuildUser)
                                    {
                                        await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.RoleAssignmentFailedForUser),
                                            contextUser.Username, role, contextUser.Guild.Name);
                                    }
                                    else
                                    {
                                        await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.RoleAssignmentFailed), role,
                                            contextUser.Guild.Name);
                                    }
                                }

                                throw p.FinalException;
                            }
                        }

                        //This we can reset using a command TODO: create this command.
                        await catchRoleNotFoundOrBreakerAndNotifyUser(async () => await roleNotFoundCircuitBreakerPolicyHelper.RoleNotFoundCircuitBreakerPolicy.ExecuteAndCaptureAsync(
                             async () => await catchRoleNotFoundOrBreakerAndNotifyUser(async () =>
                                 await roleNotFoundExceptionRetryPolicy.ExecuteAndCaptureAsync(
                                     async () => await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, contextUser,
                                         userOwnedRolesMatchingWorlds, role, bypassMessages)))));

                    }
                    else
                    {
                        if (!bypassMessages)
                        {
                            if (contextUserIsNotGuildUser)
                            {
                                await contextUser.InternalSendMessageAsync(
                                    nameof(GreyOTronResources.WorldNotInDiscordServerWorlds), guildUser.Username, guildUser.Guild.Name,
                                    gw2AccountInfo.WorldInfo.Name);
                            }
                            else
                            {
                                await contextUser.InternalSendMessageAsync(
                                    nameof(GreyOTronResources.YourWorldNotInDiscordServerWorlds),
                                    gw2AccountInfo.WorldInfo.Name, guildUser.Guild.Name);
                            }
                        }
                    }
                }
            }

            foreach (var userOwnedRolesMatchingWorld in userOwnedRolesMatchingWorlds)
            {
                try
                {
                    await guildUser.RemoveRoleAsync(userOwnedRolesMatchingWorld);
                }
                catch (Exception e)
                {
                    throw new RemoveRoleException(userOwnedRolesMatchingWorld.Name, e);
                }

            }
        }

        private async Task CreateRoleIfNotExistsAndAssignIfNeeded(SocketGuildUser guildUser, SocketGuildUser contextUser, List<SocketRole> userOwnedRolesMatchingWorlds, string roleName, bool bypassMessages)
        {
            var contextUserIsNotGuildUser = guildUser.Id != contextUser.Id;
            var roleExistsAlready = userOwnedRolesMatchingWorlds.FirstOrDefault(x =>
                string.Equals(x.Name, roleName, StringComparison.InvariantCultureIgnoreCase));
            if (roleExistsAlready == null)
            {
                var cachedName = $"roles::{guildUser.Guild.Id}::{roleName}";
                var role = cache.GetFromCacheSliding(cachedName, TimeSpan.FromDays(1), () =>
                {
                    return guildUser.Guild.Roles.FirstOrDefault(x => x.Name == roleName) ?? (IRole)guildUser.Guild.CreateRoleAsync(roleName, GuildPermissions.None, null, false, false).Result;
                });

                try
                {
                    await guildUser.AddRoleAsync(role);
                }
                catch (HttpException e)
                {
                    switch (e.HttpCode)
                    {
                        case HttpStatusCode.Forbidden:
                            if (!bypassMessages)
                            {
                                if (contextUserIsNotGuildUser)
                                {
                                    await contextUser.InternalSendMessageAsync(
                                        nameof(GreyOTronResources.NotGuildUserRoleIssue), roleName, contextUser.Guild.Name);

                                }
                                else
                                {
                                    foreach (var admin in contextUser.Guild.Users.Where(x => x.IsAdminOrOwner()))
                                    {
                                        await admin.InternalSendMessageAsync(nameof(GreyOTronResources.NotGuildUserRoleIssue),
                                            roleName, contextUser.Guild.Name);
                                    }

                                    await contextUser.InternalSendMessageAsync(
                                        nameof(GreyOTronResources.UserMessageForRoleIssue),
                                        contextUser.Guild.Name, roleName);
                                }
                            }

                            throw new RoleHierarchyException("Could not add the role to the user.", e);

                        //Notify admin of server
                        //Notify user.
                        case HttpStatusCode.NotFound:
                            cache.RemoveFromCache(cachedName);
                            cache.GetFromCacheSliding(cachedName, TimeSpan.FromDays(1),
                                () => (IRole)guildUser.Guild.CreateRoleAsync(roleName, GuildPermissions.None, null, false, false).Result);
                            throw new RoleNotFoundException(cachedName, e);
                        //await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, contextUser,
                        //    userOwnedRolesMatchingWorlds, roleName, bypassMessages);
                        default:
                            throw;
                    }
                }


                if (!bypassMessages)
                {
                    if (contextUserIsNotGuildUser)
                    {
                        await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.UserHasBeenAssignedRole),
                            guildUser.Username, roleName, guildUser.Guild.Name);
                    }
                    else
                    {
                        await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.YouHaveBeenAssignedRole), roleName,
                            guildUser.Guild.Name);
                    }
                }
            }
            else
            {
                if (!bypassMessages)
                {
                    if (contextUserIsNotGuildUser)
                    {
                        await contextUser.InternalSendMessageAsync(
                            nameof(GreyOTronResources.UserAlreadyHadRole), guildUser.Username, roleName, guildUser.Guild.Name);
                    }
                    else
                    {
                        await contextUser.InternalSendMessageAsync(
                            nameof(GreyOTronResources.YouAlreadyHadRole), roleName, guildUser.Guild.Name);
                    }
                }
            }
            userOwnedRolesMatchingWorlds.Remove(roleExistsAlready);
        }
    }
}
