﻿using GreyOTron.Library.Helpers;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using GreyOTron.Library.Exceptions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace GreyOTron.Library.ApiClients
{
    public class Gw2Api : IDisposable
    {
        private const string BaseUrl = "https://api.guildwars2.com";
        private readonly CacheHelper cache;
        private readonly DateTimeProvider dateTimeProvider;
        private readonly TimeSpanSemaphoreHelper semaphore;
        private readonly RetryPolicy timeOutOrTooManyRequestsRetryPolicy;
        private readonly RetryPolicy endpointRequiresAuthenticationRetryPolicy;
        private readonly CircuitBreakerPolicy circuitBreakerPolicy;
        public Gw2Api(CacheHelper cache, DateTimeProvider dateTimeProvider)
        {
            this.cache = cache;
            this.dateTimeProvider = dateTimeProvider;
            //Gw2API rate limits 600 reqs per minute.
            semaphore = new TimeSpanSemaphoreHelper(500, TimeSpan.FromMinutes(1)); //So we allow 500 requests every minute
            circuitBreakerPolicy = Policy.Handle<TooManyRequestsException>() //If somehow the api still says they can't handle our number of requests
                .AdvancedCircuitBreaker(
                    0.01, //when 0.01% of all requests in a timespan of 3 minutes fails we take 30s timeout before allowing to continue
                    TimeSpan.FromSeconds(180),
                    500, //Technically this could be one but we're expecting more than 500 requests before it fails anyway.
                    TimeSpan.FromSeconds(30)
                );
            timeOutOrTooManyRequestsRetryPolicy = Policy.Handle<TooManyRequestsException>().Or<ApiTimeoutException>()
                .WaitAndRetryForever(i => TimeSpan.FromSeconds(Math.Pow(2, i % 6))); //Exp back-off but with cutoff of 60s
            endpointRequiresAuthenticationRetryPolicy = Policy.Handle<EndpointRequiresAuthenticationException>()
                .WaitAndRetry(3, i => TimeSpan.FromMinutes(Math.Pow(2, i)));
        }

        public void Dispose()
        {
            semaphore.Dispose();
        }

        public AccountInfo GetInformationForUserByKey(string key, bool bypassTokenInfo = false, bool clearCache = false)
        {
            if (clearCache)
            {
                cache.RemoveFromCache($"got-user-{key}");
            }
            var accountInfo = cache.GetFromCacheAbsolute($"got-user-{key}", dateTimeProvider.UtcNow.AddHours(3), () =>
            {
                return endpointRequiresAuthenticationRetryPolicy
                    .Wrap(timeOutOrTooManyRequestsRetryPolicy)
                    .Wrap(circuitBreakerPolicy)
                    .Execute(() =>
                    {
                        var client = new RestClient(BaseUrl);
                        client.AddDefaultHeader("Authorization", $"Bearer {key}");
                        TokenInfo tokenInfo = null;
                        if (!bypassTokenInfo)
                        {
                            tokenInfo = GetTokenInfo(client, key);
                        }
                        var accountRequest = new RestRequest("v2/account");
                        IRestResponse<AccountInfo> accountResponse = null;
                        semaphore.Run(() => accountResponse = client.Execute<AccountInfo>(accountRequest, Method.GET), CancellationToken.None);
                        if (accountResponse.IsSuccessful)
                        {
                            var account = accountResponse.Data;
                            account.TokenInfo = tokenInfo;
                            var accountWorld = GetWorlds().FirstOrDefault(x => x.Id == account.World);
                            if (accountWorld != null)
                            {
                                account.WorldInfo = SetLinkedWorlds(accountWorld);
                                return account;
                            }
                        }
                        else
                        {
                            ParseResponse(accountResponse, "accountResponse", key);
                        }
                        throw new ApiInformationForUserByKeyException("Should never get here", key, null, null);
                    });
            });
            if (!bypassTokenInfo && string.IsNullOrWhiteSpace(accountInfo.TokenInfo?.Name))
            {
                var client = new RestClient(BaseUrl);
                client.AddDefaultHeader("Authorization", $"Bearer {key}");
                accountInfo.TokenInfo = GetTokenInfo(client, key);
            }
            return accountInfo;
        }

        private TokenInfo GetTokenInfo(RestClient client, string key)
        {
            IRestResponse<TokenInfo> tokenInfoResponse = null;
            var tokenInfoRequest = new RestRequest("v2/tokeninfo");
            semaphore.Run(() => tokenInfoResponse = client.Execute<TokenInfo>(tokenInfoRequest, Method.GET), CancellationToken.None);
            if (tokenInfoResponse.IsSuccessful)
            {
                return tokenInfoResponse.Data;
            }
            ParseResponse(tokenInfoResponse, "tokenInfoResponse", key);
            return null; //This won't happen because previous throws...
        }

        private void ParseResponse(IRestResponse response, string section, string key)
        {
            if (!string.IsNullOrWhiteSpace(response.Content))
            {
                string responseText;
                try
                {
                    var json = JObject.Parse(response.Content);
                    responseText = json?["text"]?.Value<string>().ToLowerInvariant() ?? string.Empty;
                }
                catch (Exception e)
                {
                    throw new ApiInformationForUserByKeyException(section, key, response.Content, e);
                }
                if (responseText == "too many requests")
                {
                    throw new TooManyRequestsException(semaphore.CurrentCount, section, key, response.Content, response.ErrorException);
                }

                if (responseText == "invalid key" || responseText == "invalid access token")
                {
                    throw new InvalidKeyException(section, key, response.Content, response.ErrorException);
                }

                if (responseText == "endpoint requires authentication")
                {
                    throw new EndpointRequiresAuthenticationException(section, key, response.Content, response.ErrorException);
                }

                if (responseText == "errtimeout")
                {
                    throw new ApiTimeoutException(section, key, response.Content, response.ErrorException);
                }
                throw new ApiInformationForUserByKeyException(section, key, response.Content, response.ErrorException);
            }
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
            var resetTime = new TimeSpan(18, 0, 0).Add(new TimeSpan(1, 55, 0)); //let's place it somewhat before reverify time.
            var daysUntilNextFriday = ((11 - (int)now.DayOfWeek) % 7) + 1;
            if (daysUntilNextFriday == 7 && now.TimeOfDay < resetTime)
            {
                daysUntilNextFriday = 0;
            }
            return new DateTimeOffset(now.Date, TimeSpan.FromHours(0)).AddDays(daysUntilNextFriday).Add(resetTime);
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
        public string Id { get; set; }
        public World WorldInfo { get; set; }
        public TokenInfo TokenInfo { get; set; }
    }


}
