using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GreyOTron.Library.TableStorage
{
    public class KeyRepository
    {
        private readonly CloudTable gw2KeysTable;

        public KeyRepository(IConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
            var greyotronClient = storageAccount.CreateCloudTableClient();
            gw2KeysTable = greyotronClient.GetTableReference("discorduserkeys");
            gw2KeysTable.CreateIfNotExistsAsync().Wait();
        }

        public async Task Set(DiscordClientWithKey key)
        {
            var insertOperation = TableOperation.InsertOrReplace(key);
            await gw2KeysTable.ExecuteAsync(insertOperation);
        }

        public async Task<DiscordClientWithKey> Get(string game, string userId)
        {
            var retrieveOperation = TableOperation.Retrieve<DiscordClientWithKey>(game, userId);
            var result = await gw2KeysTable.ExecuteAsync(retrieveOperation);
            return (DiscordClientWithKey)result.Result;
        }

        public async Task Delete(string game, string userId)
        {
            var entity = await gw2KeysTable.ExecuteAsync(TableOperation.Retrieve<DiscordClientWithKey>(game, userId));
            if (entity?.Result is DiscordClientWithKey clientWithKey)
            {
                await gw2KeysTable.ExecuteAsync(TableOperation.Delete(clientWithKey));
            }
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
