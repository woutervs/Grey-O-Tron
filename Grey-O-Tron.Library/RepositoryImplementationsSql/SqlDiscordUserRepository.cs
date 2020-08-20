using System;
using System.Threading.Tasks;
using GreyOTron.Library.Models;
using GreyOTron.Library.RepositoryInterfaces;
using Microsoft.Data.SqlClient;

namespace GreyOTron.Library.RepositoryImplementationsSql
{
    public class SqlDiscordUserRepository : IDiscordUserRepository
    {
        private readonly IDbConfiguration dbConfiguration;

        public SqlDiscordUserRepository(IDbConfiguration dbConfiguration)
        {
            this.dbConfiguration = dbConfiguration;
        }
        public async Task InsertOrUpdate(DiscordUserDto discordUser)
        {
            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);

            var sql = @"if exists(select id from got.discordusers where id = @id)
	if @preferredlanguage is not null
		update got.discordusers set [username] = @username,discriminator = @discriminator, preferredlanguage = @preferredlanguage where id = @id;
	else
		update got.discordusers set [username] = @username,discriminator = @discriminator where id = @id;
else 
	insert into got.discordusers (id, [username], discriminator, preferredlanguage) values (@id, @username, @discriminator, @preferredlanguage);";

            var command = new SqlCommand(sql, db);
            command.Parameters.AddWithValue("@id", discordUser.Id);
            command.Parameters.AddWithValue("@username", discordUser.Username);
            command.Parameters.AddWithValue("@discriminator", discordUser.Discriminator);
            command.Parameters.AddWithValue("@preferredlanguage", discordUser.PreferredLanguage.ToLowerInvariant());
        }
    }
}
