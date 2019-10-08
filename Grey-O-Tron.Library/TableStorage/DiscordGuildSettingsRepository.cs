using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GreyOTron.Library.TableStorage
{
    public class DiscordGuildSettingsRepository
    {
        private readonly CloudTable discordGuildSettings;

        public DiscordGuildSettingsRepository(IConfiguration configuration)
        {
            var storageAccount = CloudStorageAccount.Parse(configuration["StorageConnectionString"]);
            var greyotronClient = storageAccount.CreateCloudTableClient();
            discordGuildSettings = greyotronClient.GetTableReference("discordguildsettings");
            discordGuildSettings.CreateIfNotExistsAsync().Wait();
        }

        public async Task Set(DiscordGuildSetting obj)
        {
            var insertOperation = TableOperation.InsertOrReplace(obj);
            await discordGuildSettings.ExecuteAsync(insertOperation);
        }

        public async Task<DiscordGuildSetting> Get(string settingType, string guildId)
        {
            var result = await discordGuildSettings.ExecuteAsync(TableOperation.Retrieve<DiscordGuildSetting>(guildId, settingType));
            return result.Result as DiscordGuildSetting;
        }
    }

    public class DiscordGuildSetting : TableEntity
    {
        public const string Worlds = "worlds";
        public const string MainWorld = "main-world";
        public DiscordGuildSetting(string guildId,string guildName, string type, string setting)
        {
            PartitionKey = guildId;
            RowKey = type;
            GuildName = guildName;
            Value = setting;
        }

        public DiscordGuildSetting()
        {

        }

        public string GuildName { get; set; }
        public string Value { get; set; }
    }
}
