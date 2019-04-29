using GreyOTron.Library.Helpers;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;

namespace GreyOTron.Library.ApiClients
{
    public class Gw2Api : IDisposable
    {
        private const string BaseUrl = "https://api.guildwars2.com";
        private readonly Cache cache;
        private readonly TelemetryClient log;
        private readonly TimeSpanSemaphore semaphore;

        public Gw2Api(TelemetryClient log, Cache cache)
        {
            this.cache = cache;
            this.log = log;
            //Gw2API rate limits 600 reqs per minute.
            semaphore = new TimeSpanSemaphore(600, TimeSpan.FromMinutes(1));
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }

        public AccountInfo GetInformationForUserByKey(string key)
        {
            var client = new RestClient(BaseUrl);
            client.AddDefaultHeader("Authorization", $"Bearer {key}");
            IRestResponse<TokenInfo> tokenInfoResponse = null;
            var tokenInfoRequest = new RestRequest("v2/tokeninfo");
            semaphore.Run(() => tokenInfoResponse = client.Execute<TokenInfo>(tokenInfoRequest, Method.GET), CancellationToken.None);
            if (tokenInfoResponse.IsSuccessful)
            {
                var accountRequest = new RestRequest("v2/account");
                IRestResponse<AccountInfo> accountResponse = null;
                semaphore.Run(() => accountResponse = client.Execute<AccountInfo>(accountRequest, Method.GET), CancellationToken.None);
                if (accountResponse.IsSuccessful)
                {
                    var account = accountResponse.Data;
                    account.TokenInfo = tokenInfoResponse.Data;
                    account.ValidKey = true;
                    var accountWorld = GetWorlds().FirstOrDefault(x => x.Id == account.World);
                    if (accountWorld != null)
                    {
                        account.WorldInfo = SetLinkedWorlds(accountWorld);
                        return account;
                    }
                }
                if (tokenInfoResponse.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(tokenInfoResponse.Content))
                {
                    var json = JObject.Parse(tokenInfoResponse.Content);
                    var responseText = json?["text"]?.Value<string>() ?? "";
                    if (responseText.Equals("invalid key", StringComparison.InvariantCultureIgnoreCase) || responseText.Equals("endpoint requires authentication"))
                    {
                        return new AccountInfo { ValidKey = false };
                    }
                }
                else
                {
                    log.TrackException(accountResponse.ErrorException,
                        new Dictionary<string, string>
                        {
                            {"ErrorMessage", accountResponse.ErrorMessage}, {"Content", accountResponse.Content},
                            {"Section", "accountResponse"}
                        });
                }
            }
            else
            {
                if (tokenInfoResponse.StatusCode == HttpStatusCode.BadRequest && !string.IsNullOrWhiteSpace(tokenInfoResponse.Content))
                {
                    var json = JObject.Parse(tokenInfoResponse.Content);
                    var responseText = json?["text"]?.Value<string>() ?? "";
                    if (responseText.Equals("invalid key", StringComparison.InvariantCultureIgnoreCase) || responseText.Equals("endpoint requires authentication"))
                    {
                        return new AccountInfo { ValidKey = false };
                    }
                }
                else
                {
                    log.TrackException(tokenInfoResponse.ErrorException,
                        new Dictionary<string, string>
                            {{"ErrorMessage", tokenInfoResponse.ErrorMessage}, {"Content", tokenInfoResponse.Content}, {"Section","tokenInfoResponse"}});
                }
            }
            return null;
        }

        public IEnumerable<World> GetWorlds()
        {
            return cache.GetFromCacheAbsolute("worlds", CalculateNextReset(), () =>
            {
                var client = new RestClient(BaseUrl);
                var worldsRequest = new RestRequest("v2/worlds?ids=all");
                var worlds = new List<World>();
                semaphore.Run(() => worlds = client.Execute<List<World>>(worldsRequest, Method.GET).Data, CancellationToken.None);
                return worlds;
            });
        }

        private DateTimeOffset CalculateNextReset()
        {
            var now = DateTimeOffset.UtcNow;
            var daysUntilNextFriday = (DayOfWeek.Friday - now.DayOfWeek + 7) % 7;
            var nextCacheClear = new DateTimeOffset(now.Year, now.Month, now.Day, 18, 30, 0, TimeSpan.FromHours(0)).AddDays(daysUntilNextFriday);
            return nextCacheClear;
        }

        public World ParseWorld(string identifier)
        {
            return int.TryParse(identifier.Trim(' ', ';', ','), NumberStyles.Any, CultureInfo.InvariantCulture, out var worldId) ?
                GetWorlds().FirstOrDefault(x => x.Id == worldId) :
                GetWorlds().FirstOrDefault(x => x.Name.Equals(identifier.Trim(' ', ';', ','), StringComparison.InvariantCultureIgnoreCase));
        }

        private World SetLinkedWorlds(World world)
        {
            world.LinkedWorlds = cache.GetFromCacheAbsolute($"linked-worlds-for-{world.Id}", CalculateNextReset(), () =>
            {
                var client = new RestClient(BaseUrl);
                var linkedWorldsRequest = new RestRequest($"/v2/wvw/matches/overview?world={world.Id}");
                string jsonResponse = null;
                semaphore.Run(() => jsonResponse = client.Execute(linkedWorldsRequest).Content, CancellationToken.None);
                var matchInfo = JObject.Parse(jsonResponse)["all_worlds"].ToObject<MatchInfo>();
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
        public bool ValidKey { get; set; }
    }


}
