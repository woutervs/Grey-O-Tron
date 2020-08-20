using System;
using System.Threading.Tasks;
using GreyOTron.Library.Models;
using GreyOTron.Library.RepositoryInterfaces;
using Microsoft.Data.SqlClient;

namespace GreyOTron.Library.RepositoryImplementationsSql
{
    public class SqlGw2DiscordServerRepository : IGw2DiscordServerRepository
    {
        private readonly IDbConfiguration dbConfiguration;
        private readonly IDiscordServerRepository discordServerRepository;

        public SqlGw2DiscordServerRepository(IDbConfiguration dbConfiguration, IDiscordServerRepository discordServerRepository)
        {
            this.dbConfiguration = dbConfiguration;
            this.discordServerRepository = discordServerRepository;
        }
        public async Task InsertOrUpdate(Gw2DiscordServer gw2DiscordServer)
        {
            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);

            throw new NotImplementedException();
        }

        public Task<Gw2DiscordServer> Get(ulong discordId)
        {
            throw new NotImplementedException();
        }
    }
}
