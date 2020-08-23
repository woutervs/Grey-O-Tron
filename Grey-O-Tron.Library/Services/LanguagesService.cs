using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using GreyOTron.Resources;

namespace GreyOTron.Library.Services
{
    public class LanguagesService
    {
        private readonly CacheHelper cache;
        private readonly IDiscordUserRepository discordUserRepository;
        private readonly IDiscordServerRepository discordServerRepository;
        public List<CultureInfo> AvailableLanguages { get; } = new List<CultureInfo>();

        public LanguagesService(CacheHelper cache, IDiscordUserRepository discordUserRepository, IDiscordServerRepository discordServerRepository)
        {
            this.cache = cache;
            this.discordUserRepository = discordUserRepository;
            this.discordServerRepository = discordServerRepository;

            foreach (var culture in CultureInfo.GetCultures(CultureTypes.AllCultures))
            {
                try
                {
                    if (culture.Equals(CultureInfo.InvariantCulture)) { continue; }
                    var rs = GreyOTronResources.ResourceManager.GetResourceSet(culture, true, false);
                    if (rs != null)
                    {
                        AvailableLanguages.Add(culture);
                    }

                }
                catch (CultureNotFoundException)
                {
                    //
                }
            }

            if (!AvailableLanguages.Any(x => x.Name == "en"))
            {
                AvailableLanguages.Add(new CultureInfo("en"));
            }
        }

        public bool Exists(string language)
        {
            return AvailableLanguages.Any(x => x.Name.Equals(language, StringComparison.InvariantCultureIgnoreCase));
        }

        public CultureInfo GetForUserId(ulong userId)
        {
            return cache.GetFromCacheSliding($"language-for-user-{userId}", TimeSpan.FromDays(7),
                () =>
                {
                    var l = discordUserRepository.Get(userId).Result;
                    if (l != null && !string.IsNullOrWhiteSpace(l.PreferredLanguage))
                    {
                        return AvailableLanguages.Single(x => x.Name == l.PreferredLanguage);
                    }
                    return CultureInfo.InvariantCulture;
                });
        }

        public void UpdateCacheForUserId(ulong userId, string language)
        {
            cache.Replace($"language-for-user-{userId}", AvailableLanguages.Single(x => x.Name == language), TimeSpan.FromDays(7));
        }

        public CultureInfo GetForServerId(ulong serverId)
        {
            return cache.GetFromCacheSliding($"language-for-server-{serverId}", TimeSpan.FromDays(7),
                () =>
                {
                    var l = discordServerRepository.Get(serverId).Result;
                    if (l != null && string.IsNullOrWhiteSpace(l.PreferredLanguage))
                    {
                        return AvailableLanguages.Single(x => x.Name == l.PreferredLanguage);
                    }
                    return CultureInfo.InvariantCulture;
                });
        }

        public void UpdateForServerId(ulong serverId, string language)
        {
            cache.Replace($"language-for-server-{serverId}", AvailableLanguages.Single(x => x.Name == language), TimeSpan.FromDays(7));
        }
    }
}
