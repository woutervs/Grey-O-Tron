using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using GreyOTron.Library.Helpers;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace GreyOTron.Library.ApiClients
{
    public class Gw2Api
    {
        private const string BaseUrl = "https://api.guildwars2.com";
        private readonly Cache cache;
        private static readonly TelemetryClient Log = new TelemetryClient();

        public Gw2Api(Cache cache)
        {
            this.cache = cache;
        }
        public AccountInfo GetInformationForUserByKey(string key)
        {
            var client = new RestClient(BaseUrl);
            client.AddDefaultHeader("Authorization", $"Bearer {key}");
            var request = new RestRequest("v2/tokeninfo");
            var tokenInfo = client.Execute<TokenInfo>(request, Method.GET).Data;

            request = new RestRequest("v2/account");
            var account = client.Execute<AccountInfo>(request, Method.GET).Data;
            account.TokenInfo = tokenInfo;
            var accountWorld = GetWorlds().FirstOrDefault(x => x.Id == account.World);
            if (accountWorld == null)
            {
                Log.TrackTrace("No world found for user", new Dictionary<string, string> { { "account", JsonConvert.SerializeObject(account) } });
                return account;
            }
            account.WorldInfo = SetLinkedWorlds(accountWorld);
            return account;
        }

        private IEnumerable<World> GetWorlds()
        {
            return cache.GetFromCache("worlds", TimeSpan.FromDays(1), () =>
            {
                var client = new RestClient(BaseUrl);
                var request = new RestRequest("v2/worlds?ids=all");
                return client.Execute<List<World>>(request, Method.GET).Data;
            });
        }

        public World ParseWorld(string identifier)
        {
            return int.TryParse(identifier.Trim(' ', ';', ','), NumberStyles.Any, CultureInfo.InvariantCulture, out var worldId) ?
                GetWorlds().FirstOrDefault(x => x.Id == worldId) :
                GetWorlds().FirstOrDefault(x => x.Name.Equals(identifier.Trim(' ', ';', ','), StringComparison.InvariantCultureIgnoreCase));
        }

        private World SetLinkedWorlds(World world)
        {
            world.LinkedWorlds = cache.GetFromCache($"linked-worlds-for-{world.Id}", TimeSpan.FromDays(1), () =>
            {
                var client = new RestClient(BaseUrl);
                var request = new RestRequest($"/v2/wvw/matches/overview?world={world.Id}");
                var restResponse = client.Execute(request).Content;
                var matchInfo = JObject.Parse(restResponse)["all_worlds"].ToObject<MatchInfo>();
                return matchInfo.FindLinksFor(world.Id).Select(x => GetWorlds().FirstOrDefault(y => y.Id == x)).ToList();
            });
            return world;
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class MatchInfo
        {
            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<int> Red { get; set; }
            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<int> Blue { get; set; }
            // ReSharper disable once MemberCanBePrivate.Local
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public List<int> Green { get; set; }

            public IEnumerable<int> FindLinksFor(int id)
            {
                return (Red.Contains(id) ? Red : Green.Contains(id) ? Green : Blue).Where(x => x != id);
            }
        }
    }

    public class TokenInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }

    public class World
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<World> LinkedWorlds { get; set; } = new List<World>();
    }

    public class AccountInfo
    {
        public int World { get; set; }
        public World WorldInfo { get; set; }
        public TokenInfo TokenInfo { get; set; }
    }


}
