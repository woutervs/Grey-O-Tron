using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GreyOTron.TableStorage
{
    public class KeyRepository
    {
        private readonly CloudTable _gw2KeysTable;

        public KeyRepository(IConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
            var greyotronClient = storageAccount.CreateCloudTableClient();
            _gw2KeysTable = greyotronClient.GetTableReference("discorduserkeys");
            _gw2KeysTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task Set(DiscordClientWithKey key)
        {
            var insertOperation = TableOperation.InsertOrReplace(key);
            await _gw2KeysTable.ExecuteAsync(insertOperation);
        }

        public async Task<DiscordClientWithKey> Get(string game, string userId)
        {
            var retrieveOperation = TableOperation.Retrieve<DiscordClientWithKey>(game, userId);
            var result = await _gw2KeysTable.ExecuteAsync(retrieveOperation);
            return (DiscordClientWithKey)result.Result;
        }

        public async Task<List<DiscordClientWithKey>> Get(string game)
        {
            var query = new TableQuery<DiscordClientWithKey>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, game));
            TableContinuationToken continuationToken = null;
            var clients = new List<DiscordClientWithKey>();
            do
            {
                var result = await _gw2KeysTable.ExecuteQuerySegmentedAsync(query, continuationToken);
                clients.AddRange(result.Results);
                continuationToken = result.ContinuationToken;

            } while (continuationToken != null);

            return clients;
        }

    }

    public class DiscordClientWithKey : TableEntity
    {
        public DiscordClientWithKey(string game, string userId, string username, string key)
        {
            PartitionKey = game;
            RowKey = userId;
            Key = key;
            Username = username;
        }

        public DiscordClientWithKey()
        {

        }

        public string Username { get; set; }
        public string Key { get; set; }
    }
}
