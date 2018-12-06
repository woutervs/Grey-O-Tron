using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GreyOTron.TableStorage
{
    public class DiscordGuildGw2WorldRepository
    {
        private readonly CloudTable _discordGuildGw2WorldTable;

        public DiscordGuildGw2WorldRepository(IConfigurationRoot configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
            var greyotronClient = storageAccount.CreateCloudTableClient();
            _discordGuildGw2WorldTable = greyotronClient.GetTableReference("discordguildgw2world");
            _discordGuildGw2WorldTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task Set(DiscordGw2World obj)
        {
            var insertOperation = TableOperation.InsertOrReplace(obj);
            await _discordGuildGw2WorldTable.ExecuteAsync(insertOperation);
        }

        public async Task Clear(string guildId)
        {
            var query = new TableQuery<DiscordGw2World>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, guildId));
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await _discordGuildGw2WorldTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = result.ContinuationToken;
                foreach (var entity in result.Results)
                {
                    await _discordGuildGw2WorldTable.ExecuteAsync(TableOperation.Delete(entity));
                }
            } while (continuationToken != null);
        }

        public async Task<List<DiscordGw2World>> Get(string guildId)
        {
            var query = new TableQuery<DiscordGw2World>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, guildId));
            TableContinuationToken continuationToken = null;
            var worlds = new List<DiscordGw2World>();
            do
            {
                var result = await _discordGuildGw2WorldTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = result.ContinuationToken;
                worlds.AddRange(result.Results);
            } while (continuationToken != null);

            return worlds;
        }
    }

    public class DiscordGw2World : TableEntity
    {
        public DiscordGw2World(string guildId, string world)
        {
            PartitionKey = guildId;
            RowKey = world;
        }

        public DiscordGw2World()
        {

        }
    }
}
