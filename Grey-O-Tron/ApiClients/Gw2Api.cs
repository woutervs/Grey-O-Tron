﻿using System;
using System.Collections.Generic;
using System.Linq;
using GreyOTron.Helpers;
using RestSharp;

namespace GreyOTron.ApiClients
{
    public class Gw2Api
    {
        private readonly Cache cache;

        public Gw2Api(Cache cache)
        {
            this.cache = cache;
        }
        public AccountInfo GetInformationForUserByKey(string key)
        {
            var client = new RestClient("https://api.guildwars2.com");
            client.AddDefaultHeader("Authorization", $"Bearer {key}");
            var request = new RestRequest("v2/tokeninfo");
            var tokenInfo = client.Execute<TokenInfo>(request, Method.GET).Data;
            var worlds = cache.GetFromCache("worlds", TimeSpan.FromDays(1), () =>
            {
                request = new RestRequest("v2/worlds?ids=all");
                return client.Execute<List<World>>(request, Method.GET).Data;
            });
            request = new RestRequest("v2/account");
            var account = client.Execute<AccountInfo>(request, Method.GET).Data;
            account.TokenInfo = tokenInfo;
            account.WorldInfo = worlds.FirstOrDefault(x => x.Id == account.World);
            return account;
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
    }

    public class AccountInfo
    {
        public int World { get; set; }
        public World WorldInfo { get; set; }
        public TokenInfo TokenInfo { get; set; }
    }
}