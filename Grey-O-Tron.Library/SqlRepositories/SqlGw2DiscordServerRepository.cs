using System.Linq;
using System.Threading.Tasks;
using Dapper;
using GreyOTron.Library.Interfaces;
using GreyOTron.Library.Models;
using Microsoft.Data.SqlClient;

namespace GreyOTron.Library.SqlRepositories
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
            await discordServerRepository.InsertOrUpdate(gw2DiscordServer.DiscordServer);

            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);
            await db.OpenAsync();

            if (gw2DiscordServer.MainWorld != null)
            {
                var sql = @"if exists(select discordserverid from got.DiscordServersGw2MainWorld where discordserverid = @discordserverid)
	update got.DiscordServersGw2MainWorld set gw2worldid = @gw2worldid where discordserverid = @discordserverid;
else 
	insert into got.DiscordServersGw2MainWorld (discordserverid, gw2worldid) values (@discordserverid, @gw2worldid);
";
                var command = new SqlCommand(sql, db);
                command.Parameters.AddWithValue("discordserverid", (decimal) gw2DiscordServer.DiscordServer.Id);
                command.Parameters.AddWithValue("gw2worldid", gw2DiscordServer.MainWorld.Id);
                await command.ExecuteNonQueryAsync();
            }

            if (gw2DiscordServer.Worlds != null)
            {
                var dSql = @"delete from got.DiscordServersGw2Worlds where discordserverid = @discordserverid";
                var dCommand = new SqlCommand(dSql, db);
                dCommand.Parameters.AddWithValue("discordserverid", (decimal) gw2DiscordServer.DiscordServer.Id);
                await dCommand.ExecuteNonQueryAsync();

                foreach (var gw2WorldDto in gw2DiscordServer.Worlds)
                {
                    var iSql = @"insert into got.DiscordServersGw2Worlds (discordserverid, gw2worldid) values (@discordserverid,  @gw2worldid)";
                    var iCommand = new SqlCommand(iSql, db);
                    iCommand.Parameters.AddWithValue("discordserverid", (decimal)gw2DiscordServer.DiscordServer.Id);
                    iCommand.Parameters.AddWithValue("gw2worldid", gw2WorldDto.Id);
                    await iCommand.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<Gw2DiscordServer> Get(ulong discordId)
        {
            var discordServer = await discordServerRepository.Get(discordId);
            if (discordServer == null)
            {
                return null;
            }

            await using var db = new SqlConnection(dbConfiguration.ConnectionString);
            dbConfiguration.AuthenticateDbConnection(db);

            var gw2DiscordServer = new Gw2DiscordServer
            {
                DiscordServer = discordServer,
                MainWorld = await db.QuerySingleOrDefaultAsync<Gw2WorldDto>(@"select w.id, w.name from got.DiscordServersGw2MainWorld dw
inner join got.gw2worlds w on w.id = dw.gw2worldid
where dw.discordserverid = @discordId", new { discordId = (decimal) discordId }),
                Worlds = (await db.QueryAsync<Gw2WorldDto>(@"select w.id, w.name from got.DiscordServersGw2Worlds dw 
inner join got.gw2worlds w on w.id = dw.gw2worldid
where dw.discordserverid = @discordId", new { discordId = (decimal) discordId })).ToList()
            };

            return gw2DiscordServer;
        }
    }
}
