using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Exceptions;
using GreyOTron.Library.TableStorage;
using GreyOTron.Library.Translations;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;

namespace GreyOTron.Library.Helpers
{
    public class VerifyUser
    {
        private readonly DiscordGuildSettingsRepository discordGuildSettingsRepository;
        private readonly Cache cache;
        private readonly IConfiguration configuration;
        private readonly RoleNotFoundCircuitBreakerPolicyHelper roleNotFoundCircuitBreakerPolicyHelper;
        private readonly AsyncRetryPolicy roleNotFoundExceptionRetryPolicy;

        public VerifyUser(DiscordGuildSettingsRepository discordGuildSettingsRepository, Cache cache, IConfiguration configuration, RoleNotFoundCircuitBreakerPolicyHelper roleNotFoundCircuitBreakerPolicyHelper)
        {
            this.discordGuildSettingsRepository = discordGuildSettingsRepository;
            this.cache = cache;
            this.configuration = configuration;
            this.roleNotFoundCircuitBreakerPolicyHelper = roleNotFoundCircuitBreakerPolicyHelper;
            roleNotFoundExceptionRetryPolicy = Policy.Handle<RoleNotFoundException>()
                .WaitAndRetryAsync(1, x => TimeSpan.FromSeconds(x * 30));
        }

        public async Task Execute(AccountInfo gw2AccountInfo, SocketGuildUser guildUser, SocketGuildUser contextUser, bool bypassMessages = false)
        {
            var contextUserIsNotGuildUser = guildUser.Id != contextUser.Id;

            var worlds = JsonConvert.DeserializeObject<List<string>>((await discordGuildSettingsRepository.Get(DiscordGuildSetting.Worlds, guildUser.Guild.Id.ToString()))?.Value ?? "[]");
            var mainWorld =
                (await discordGuildSettingsRepository.Get(DiscordGuildSetting.MainWorld, guildUser.Guild.Id.ToString()))?.Value;

            if (mainWorld != null && !worlds.Contains(mainWorld))
            {
                worlds.Add(mainWorld);
            }

            var userOwnedRolesMatchingWorlds = guildUser.Roles.Where(x => worlds.Contains(x.Name.ToLowerInvariant()) || x.Name.Equals(configuration["LinkedServerRole"], StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (gw2AccountInfo.TokenInfo.Name != $"{guildUser.Username}#{guildUser.Discriminator}")
            {
                if (contextUserIsNotGuildUser)
                {
                    await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.UserChangedDiscordUsername),
                        guildUser.Nickname,
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
                                nameof(GreyOTronResources.ApiFailedToReturnWorldsForUser), guildUser.Nickname);
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
                    if (worlds.Contains(gw2AccountInfo.WorldInfo.Name.ToLowerInvariant()))
                    {
                        role = gw2AccountInfo.WorldInfo.Name;

                    }
                    else if (gw2AccountInfo.WorldInfo.LinkedWorlds.Any(x => string.Equals(x.Name, mainWorld, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        role = configuration["LinkedServerRole"];

                    }
                    if (role != null)
                    {
                        async Task catchRoleNotFoundOrBreakerAndNotifyUser(Action action)
                        {
                            try
                            {
                                action();
                            }
                            catch (Exception)
                            {
                                if (!bypassMessages)
                                {
                                    if (contextUserIsNotGuildUser)
                                    {
                                        await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.RoleAssignmentFailedForUser),
                                            contextUser.Nickname, role, contextUser.Guild.Name);
                                    }
                                    else
                                    {
                                        await contextUser.InternalSendMessageAsync(nameof(GreyOTronResources.RoleAssignmentFailed), role,
                                            contextUser.Guild.Name);
                                    }
                                }
                                throw;
                            }
                        }

                        //This we can reset using a command TODO: create this command.
                        ;
                        await catchRoleNotFoundOrBreakerAndNotifyUser(async () => await roleNotFoundCircuitBreakerPolicyHelper.RoleNotFoundCircuitBreakerPolicy.ExecuteAsync(
                            async () => await catchRoleNotFoundOrBreakerAndNotifyUser(async () =>
                            await roleNotFoundExceptionRetryPolicy.ExecuteAsync(
                                async () => await CreateRoleIfNotExistsAndAssignIfNeeded(guildUser, contextUser, userOwnedRolesMatchingWorlds, role, bypassMessages)))));

                    }
                    else
                    {
                        if (!bypassMessages)
                        {
                            if (contextUserIsNotGuildUser)
                            {
                                await contextUser.InternalSendMessageAsync(
                                    nameof(GreyOTronResources.WorldNotInDiscordServerWorlds), guildUser.Nickname, guildUser.Guild.Name,
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
                    return guildUser.Guild.Roles.FirstOrDefault(x => x.Name == roleName) ?? (IRole)guildUser.Guild.CreateRoleAsync(roleName, GuildPermissions.None).Result;
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
                                () => (IRole)guildUser.Guild.CreateRoleAsync(roleName, GuildPermissions.None).Result);
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
                            guildUser.Nickname, roleName, guildUser.Guild.Name);
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
                            nameof(GreyOTronResources.UserAlreadyHadRole), guildUser.Nickname, roleName, guildUser.Guild.Name);
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
