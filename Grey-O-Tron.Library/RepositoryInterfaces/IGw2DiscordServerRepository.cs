using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.RepositoryInterfaces
{
    public interface IGw2DiscordServerRepository
    {
        //private readonly CloudTable discordGuildSettings;

        //public IGw2DiscordServerRepository(IConfiguration configuration)
        //{
        //    var storageAccount = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
        //    var greyotronClient = storageAccount.CreateCloudTableClient();
        //    discordGuildSettings = greyotronClient.GetTableReference("discordguildsettings");
        //    discordGuildSettings.CreateIfNotExistsAsync().Wait();
        //}

        Task InsertOrUpdate(Gw2DiscordServer gw2DiscordServer);

        Task<Gw2DiscordServer> Get(ulong discordId);

        //Task Set(DiscordGuildSetting obj);
        //{
        //    var insertOperation = TableOperation.InsertOrReplace(obj);
        //    await discordGuildSettings.ExecuteAsync(insertOperation);
        //}

        //Task<DiscordGuildSetting> Get(string settingType, string guildId);
        //{
        //    var result = await discordGuildSettings.ExecuteAsync(TableOperation.Retrieve<DiscordGuildSetting>(guildId, settingType));
        //    return result.Result as DiscordGuildSetting;
        //}
    }

    //public class DiscordGuildSetting : TableEntity
    //{
    //    public const string Worlds = "worlds";
    //    public const string MainWorld = "main-world";
    //    public DiscordGuildSetting(string guildId,string guildName, string type, string setting)
    //    {
    //        PartitionKey = guildId;
    //        RowKey = type;
    //        GuildName = guildName;
    //        Value = setting;
    //    }

    //    public DiscordGuildSetting()
    //    {

    //    }

    //    public string GuildName { get; set; }
    //    public string Value { get; set; }
    //}
}
