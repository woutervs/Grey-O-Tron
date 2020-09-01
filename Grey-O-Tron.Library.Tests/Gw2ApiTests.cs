using System;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using FakeItEasy;
using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using GreyOTron.Library.Interfaces;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Data.SqlClient;
using RestSharp;
using Xunit;
using Xunit.Abstractions;

namespace GreyOTron.Library.Tests
{
    public class Gw2ApiTests
    {
        private readonly ITestOutputHelper outputHelper;

        public Gw2ApiTests(ITestOutputHelper outputHelper)
        {
            this.outputHelper = outputHelper;
        }

        [Fact]
        public void Test_Gw2Api()
        {
            var api = new Gw2Api(new CacheHelper(), new DateTimeProvider());
            api.GetInformationForUserByKey("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXXXXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX");
        }

        [Theory]
        [GreyOTronLibraryAutoData]
        public async Task UpdateMissingAccountIds(IDbConfiguration dbConfiguration)
        {
            var sqlConnectionString = "Server=greyotron.database.windows.net,1433;Database=greyotron";
            await using (var db = new SqlConnection(sqlConnectionString))
            {
                A.CallTo(() => dbConfiguration.AuthenticateDbConnection).Invokes(() =>
                {
                    db.Credential = null;
                    db.AccessToken = new AzureServiceTokenProvider()
                        .GetAccessTokenAsync("https://database.windows.net/", "c9aa12d9-958d-4e12-9e83-ada91e19f0be").Result;
                });
                dbConfiguration.AuthenticateDbConnection(db);
                await db.OpenAsync();

                var discordUsers =
                    await db.QueryAsync<Gw2DiscordUserDto>(
                        "select * FROM [got].[DiscordUserGw2ApiKeys] where gw2accountid is null");
                var semaphore = new TimeSpanSemaphoreHelper(500, TimeSpan.FromMinutes(1));

                foreach (var discordUser in discordUsers)
                {
                    var client = new RestClient("https://api.guildwars2.com");
                    client.AddDefaultHeader("Authorization", $"Bearer {discordUser.ApiKey}");
                    IRestResponse<AccountInfo> accountInfoResponse = null;
                    var tokenInfoRequest = new RestRequest("v2/account");
                    semaphore.Run(() => accountInfoResponse = client.Execute<AccountInfo>(tokenInfoRequest, Method.GET),
                        CancellationToken.None);
                    if (accountInfoResponse.IsSuccessful)
                    {
                        var accountInfo = accountInfoResponse.Data;
                        var sql =
                            "update got.DiscordUserGw2ApiKeys set gw2accountid=@accountId where discorduserid=@discordUserId";
                        var command = new SqlCommand(sql, db);
                        command.Parameters.AddWithValue("accountId", accountInfo.Id);
                        command.Parameters.AddWithValue("discordUserId", discordUser.DiscordUserId);
                        await command.ExecuteNonQueryAsync();
                        outputHelper.WriteLine($"Updated {discordUser.DiscordUserId} with {accountInfo.Id} on {DateTime.Now}");
                    }
                    else
                    {
                        if (accountInfoResponse.Content.Contains("invalid key", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var sql =
                                "delete from got.DiscordUserGw2ApiKeys where discorduserid=@discordUserId";
                            var command = new SqlCommand(sql, db);
                            command.Parameters.AddWithValue("discordUserId", discordUser.DiscordUserId);
                            await command.ExecuteNonQueryAsync();
                            outputHelper.WriteLine($"Deleted {discordUser.DiscordUserId} on {DateTime.Now}");
                        }
                        else
                        {
                            outputHelper.WriteLine($"Failed {discordUser.DiscordUserId} on {DateTime.Now}\nReason: {accountInfoResponse.Content}");
                        }
                        
                    }

                    //ParseResponse(tokenInfoResponse, "tokenInfoResponse", key);
                    //return null; //This won't happen because previous throws...
                    //		var un = setting.Username.Split('#');
                    //		var username = un[0];
                    //		var discriminator = un[1];
                    //		
                    //		var sqlDiscordUsers = "insert into got.DiscordUsers (id, username, discriminator) values (@id, @username, @discriminator)";
                    //		var commandDiscordUsers = new SqlCommand(sqlDiscordUsers, sqlClient);
                    //		commandDiscordUsers.Parameters.AddWithValue("@id", setting.RowKey);
                    //		commandDiscordUsers.Parameters.AddWithValue("@username", username);
                    //		commandDiscordUsers.Parameters.AddWithValue("@discriminator", discriminator);
                    //		await commandDiscordUsers.ExecuteNonQueryAsync();
                    //
                    //		var sqlDiscordUserGw2ApiKeys = "insert into got.DiscordUserGw2ApiKeys (discorduserid,apikey) values (@id,@key)";
                    //		var commandDiscordServersGw2MainWorld = new SqlCommand(sqlDiscordUserGw2ApiKeys, sqlClient);
                    //		commandDiscordServersGw2MainWorld.Parameters.AddWithValue("@id", setting.RowKey);
                    //		commandDiscordServersGw2MainWorld.Parameters.AddWithValue("@key", setting.Key);
                    //		await commandDiscordServersGw2MainWorld.ExecuteNonQueryAsync();
                }
            }
        }
        private class Gw2DiscordUserDto
        {
            public long DiscordUserId { get; set; }
            public string ApiKey { get; set; }

            public string Gw2AccountId { get; set; }
        }
    }
}
