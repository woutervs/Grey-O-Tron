using System;
using System.Threading.Tasks;
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

            var sql = @"if exists(select id from got.discordusergw2apikeys where discorduserid = @discorduserid)
	if @gw2accountid is not null
		update got.discordusergw2apikeys set apikey = @apikey, gw2accountid = @gw2accountid  where discorduserid = @discorduserid;
	else
		update got.discordusergw2apikeys set apikey = @apikey  where discorduserid = @discorduserid;
else 
	insert into got.discordusergw2apikeys (discorduserid, apikey, gw2accountid) values (@discorduserid, @apikey, @discorduserid);";

            var command = new SqlCommand(sql, db);
            command.Parameters.AddWithValue("@discorduserid", gw2DiscordUser.DiscordUserDto.Id);
            command.Parameters.AddWithValue("@apikey", gw2DiscordUser.ApiKey);
            command.Parameters.AddWithValue("@gw2accountid", gw2DiscordUser.Gw2AccountId);
        }

        public Task<Gw2DiscordUser> Get(ulong userId)
        {
            throw new NotImplementedException();
        }

        public Task RemoveApiKey(ulong userId)
        {
            throw new NotImplementedException();
        }
    }
}
