using System;
using System.Threading.Tasks;
using GreyOTron.Library.Models;
using GreyOTron.Library.RepositoryInterfaces;
using Microsoft.Data.SqlClient;

namespace GreyOTron.Library.RepositoryImplementationsSql
{
    public class SqlDiscordServerRepository : IDiscordServerRepository
    {
        private readonly IDbConfiguration dbConfiguration;

        public SqlDiscordServerRepository(IDbConfiguration dbConfiguration)
        {
            this.dbConfiguration = dbConfiguration;
        }
        public async Task InsertOrUpdate(DiscordServerDto discordServer)
        {
            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);

            var sql = @"if exists(select id from got.discordservers where id = @id)
	if @preferredlanguage is not null
		update got.discordservers set [name] = @name, preferredlanguage = @preferredlanguage where id = @id;
	else
		update got.discordservers set [name] = @name where id = @id;
else 
	if @preferredlanguage is not null
		insert into got.discordservers (id, [name], preferredlanguage) values (@id, @name, @preferredlanguage);
	else
		insert into got.discordservers (id, [name]) values (@id, @name);";

            var command =  new SqlCommand(sql, db);
            command.Parameters.AddWithValue("@id", discordServer.Id);
            command.Parameters.AddWithValue("@name", discordServer.Name);
            command.Parameters.AddWithValue("@preferredlanguage", discordServer.PreferredLanguage.ToLowerInvariant());

            await command.ExecuteNonQueryAsync();
        }
    }
}
