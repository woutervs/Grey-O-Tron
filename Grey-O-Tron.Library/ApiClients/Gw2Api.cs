using GreyOTron.Library.Helpers;
using Microsoft.ApplicationInsights;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace GreyOTron.Library.ApiClients
{
    public class Gw2Api : IDisposable
    {
        private const string BaseUrl = "https://api.guildwars2.com";
        private readonly Cache cache;
        private readonly TelemetryClient log;
        private readonly TimeSpanSemaphore semaphore;
        private readonly RetryPolicy retryPolicy;
        private readonly CircuitBreakerPolicy circuitBreakerPolicy;
        public Gw2Api(TelemetryClient log, Cache cache)
        {
            this.cache = cache;
            this.log = log;
            //Gw2API rate limits 600 reqs per minute.
            semaphore = new TimeSpanSemaphore(500, TimeSpan.FromMinutes(1)); //So we allow 500 requests every minute
            circuitBreakerPolicy = Policy.Handle<TooManyRequestsException>() //If somehow the api still says they can't handle our number of requests
                .AdvancedCircuitBreaker(
                    0.01, //when 0.01% of all requests in a timespan of 3 minutes fails we take 30s timeout before allowing to continue
                    TimeSpan.FromSeconds(180),
                    500, //Technically this could be one but we're expecting more than 500 requests before it fails anyway.
                    TimeSpan.FromSeconds(30)
                );
            retryPolicy = Policy.Handle<TooManyRequestsException>()
                .WaitAndRetryForever(i => TimeSpan.FromSeconds(Math.Pow(2, i % 6))); //Exp back-off but with cutoff of 60s
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }

        public AccountInfo GetInformationForUserByKey(string key)
        {
            return retryPolicy.Wrap(circuitBreakerPolicy).Execute(() =>
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
                    else
                    {
                        ParseResponse(accountResponse, "accountResponse", out var responseText);
                        if (responseText == "invalid key" || responseText == "endpoint requires authentication")
                        {
                            return new AccountInfo { ValidKey = false };
                        }
                    }
                }
                else
                {
                    ParseResponse(tokenInfoResponse, "tokenInfoResponse", out var responseText);
                    if (responseText == "invalid key" || responseText == "endpoint requires authentication")
                    {
                        return new AccountInfo { ValidKey = false };
                    }
                }
                return null;
            });
        }

        private void ParseResponse(IRestResponse response, string section, out string responseText)
        {
            responseText = string.Empty;
            var dict = new Dictionary<string, string>
            {
                {"ErrorMessage", response.ErrorMessage}, {"Content", response.Content},
                {"Section", section}
            };

            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                var json = JObject.Parse(response.Content);
                responseText = json?["text"]?.Value<string>().ToLowerInvariant() ?? string.Empty;

                if (responseText == "too many requests")
                {
                    dict.Add("SemaphoreCount", semaphore.CurrentCount.ToString());
                    throw new TooManyRequestsException();
                }
            }
            log.TrackException(response.ErrorException, dict);
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
