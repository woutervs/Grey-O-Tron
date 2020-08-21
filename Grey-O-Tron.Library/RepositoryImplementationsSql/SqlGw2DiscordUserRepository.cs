using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GreyOTron.Library.Models;
using GreyOTron.Library.RepositoryInterfaces;
using Microsoft.Data.SqlClient;

namespace GreyOTron.Library.RepositoryImplementationsSql
{
    public class SqlGw2DiscordUserRepository : IGw2DiscordUserRepository
    {
        private readonly IDbConfiguration dbConfiguration;
        private readonly IDiscordUserRepository discordUserRepository;

        public SqlGw2DiscordUserRepository(IDbConfiguration dbConfiguration, IDiscordUserRepository discordUserRepository)
        {
            this.dbConfiguration = dbConfiguration;
            this.discordUserRepository = discordUserRepository;
        }
        public async Task InsertOrUpdate(Gw2DiscordUser gw2DiscordUser)
        {
            await discordUserRepository.InsertOrUpdate(gw2DiscordUser.DiscordUserDto);

            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);
            await db.OpenAsync();

            var sql = @"if exists(select discorduserid from got.discordusergw2apikeys where discorduserid = @discorduserid)
	if @gw2accountid is not null
		update got.discordusergw2apikeys set apikey = @apikey, gw2accountid = @gw2accountid  where discorduserid = @discorduserid;
	else
		update got.discordusergw2apikeys set apikey = @apikey  where discorduserid = @discorduserid;
else 
	insert into got.discordusergw2apikeys (discorduserid, apikey, gw2accountid) values (@discorduserid, @apikey, @gw2accountid);";

            var command = new SqlCommand(sql, db);
            command.Parameters.AddWithValue("@discorduserid", (decimal) gw2DiscordUser.DiscordUserDto.Id);
            command.Parameters.AddWithValue("@apikey", gw2DiscordUser.ApiKey);
            command.Parameters.AddWithValue("@gw2accountid", gw2DiscordUser.Gw2AccountId);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<Gw2DiscordUser> Get(ulong userId)
        {
            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);


            var result = await db.QueryAsync<DiscordUserDto, Gw2DiscordUser, Gw2DiscordUser>(@"select * from got.discordusers du
inner join got.discordusergw2apikeys dugw2 on du.id = dugw2.discorduserid
where id = @userId", (dto, user) => { user.DiscordUserDto = dto;
                return user;
            }, new {userId = (decimal) userId}, splitOn: "discorduserid");
            return result.Distinct().SingleOrDefault();
        }

        public async Task RemoveApiKey(ulong userId)
        {
            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);
            await db.OpenAsync();

            var sql = @"delete from got.discordusergw2apikeys where discorduserid = @discorduserid";
            var command = new SqlCommand(sql, db);
            command.Parameters.AddWithValue("@discorduserid", (decimal) userId);
            await command.ExecuteNonQueryAsync();
        }
    }
}
