using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GreyOTron
{
    public class Gw2KeyRepository
    {
        private readonly CloudTable _gw2KeysTable;

        public Gw2KeyRepository(string connectionString)
        {
            var storageAccount = CloudStorageAccount.Parse(connectionString);
            var greyotronClient = storageAccount.CreateCloudTableClient();
            _gw2KeysTable = greyotronClient.GetTableReference("gw2keys");
            _gw2KeysTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task Set(DiscordClientWithKey key)
        {
            var insertOperation = TableOperation.InsertOrReplace(key);
            await _gw2KeysTable.ExecuteAsync(insertOperation);
        }

        public async Task<DiscordClientWithKey> Get(string guildId, string userId)
        {
            var retrieveOperation = TableOperation.Retrieve<DiscordClientWithKey>(guildId, userId);
            var result = await _gw2KeysTable.ExecuteAsync(retrieveOperation);
            return (DiscordClientWithKey)result.Result;
        }

        public async Task<List<DiscordClientWithKey>> Get(string guildId)
        {
            var query = new TableQuery<DiscordClientWithKey>().Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, guildId));
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
        public DiscordClientWithKey(string guildId, string userId, string username, string gw2Key)
        {
            PartitionKey = guildId;
            RowKey = userId;
            Gw2Key = gw2Key;
            Username = username;
        }

        public DiscordClientWithKey()
        {

        }

        public string Username { get; set; }
        public string Gw2Key { get; set; }
    }
}
