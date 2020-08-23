﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;

namespace GreyOTron.Library.Services
{
    public class LanguagesService
    {
        private readonly CacheHelper cache;
        private readonly IDiscordUserRepository discordUserRepository;
        private readonly IDiscordServerRepository discordServerRepository;

        public LanguagesService(CacheHelper cache, IDiscordUserRepository discordUserRepository, IDiscordServerRepository discordServerRepository)
        {
            this.cache = cache;
            this.discordUserRepository = discordUserRepository;
            this.discordServerRepository = discordServerRepository;
        }

        public List<string> Get()
        {
            //TODO: resolve this once from resourcemanager.
            return new List<string>
            {
                "en", "nl", "fr", "de"
            };
        }

        public bool Exists(string language)
        {
            return Get().Any(x => x.Equals(language, StringComparison.InvariantCultureIgnoreCase));
        }

        public CultureInfo GetForUserId(ulong userId)
        {
            var language = cache.GetFromCacheSliding($"language-for-user-{userId}", TimeSpan.FromDays(7), () => discordUserRepository.Get(userId).Result?.PreferredLanguage ?? "notset");
            try
            {
                return CultureInfo.GetCultureInfoByIetfLanguageTag(language);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void UpdateForUserId(ulong userId, string language)
        {
            cache.Replace($"language-for-user-{userId}", language, TimeSpan.FromDays(7));
        }

        public CultureInfo GetForServerId(ulong serverId)
        {
            var language = cache.GetFromCacheSliding($"language-for-server-{serverId}", TimeSpan.FromDays(7),
                () => discordServerRepository.Get(serverId).Result?.PreferredLanguage ?? "notset");
            try
            {
                return CultureInfo.GetCultureInfoByIetfLanguageTag(language);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void UpdateForServerId(ulong serverId, string language)
        {
            cache.Replace($"language-for-server-{serverId}", language, TimeSpan.FromDays(7));
        }
    }
}
