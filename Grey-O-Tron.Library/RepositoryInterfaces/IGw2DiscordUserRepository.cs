using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.RepositoryInterfaces
{
    public interface IGw2DiscordUserRepository
    {
        //private readonly CloudTable gw2KeysTable;

        //public IKeyRepository(IConfiguration configuration)
        //{
        //    var storageAccount = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
        //    var greyotronClient = storageAccount.CreateCloudTableClient();
        //    gw2KeysTable = greyotronClient.GetTableReference("discorduserkeys");
        //    gw2KeysTable.CreateIfNotExistsAsync().Wait();
        //}

        Task InsertOrUpdate(Gw2DiscordUser gw2DiscordUser);
        //{
        //    //var insertOperation = TableOperation.InsertOrReplace(key);
        //    //await gw2KeysTable.ExecuteAsync(insertOperation);
        //}

        Task<Gw2DiscordUser> Get(ulong userId);
        //{
        //    //var retrieveOperation = TableOperation.Retrieve<object>(game, userId);
        //    //var result = await gw2KeysTable.ExecuteAsync(retrieveOperation);
        //    //return (DiscordClientWithKey)result.Result;
        //}

        Task RemoveApiKey(ulong userId);
        //{
        //    //var entity = await gw2KeysTable.ExecuteAsync(TableOperation.Retrieve<DiscordClientWithKey>(game, userId));
        //    //if (entity?.Result is DiscordClientWithKey clientWithKey)
        //    //{
        //    //    await gw2KeysTable.ExecuteAsync(TableOperation.RemoveApiKey(clientWithKey));
        //    //}
        //}

    }

    //public class DiscordClientWithKey : TableEntity
    //{
    //    public DiscordClientWithKey(string game, string userId, string username, string key)
    //    {
    //        PartitionKey = game;
    //        RowKey = userId;
    //        Key = key;
    //        Username = username;
    //    }

    //    public DiscordClientWithKey()
    //    {

    //    }

    //    public string Username { get; set; }
    //    public string Key { get; set; }
    //}
}
