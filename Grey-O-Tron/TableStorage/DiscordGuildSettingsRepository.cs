using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace GreyOTron.TableStorage
{
    public class DiscordGuildSettingsRepository
    {
        private readonly CloudTable discordGuildSettings;

        public DiscordGuildSettingsRepository(IConfigurationRoot configuration)
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

        public async Task Clear(string settingType, string guildId)
        {
            var query = new TableQuery<DiscordGuildSetting>().Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, settingType), TableOperators.And, TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, guildId)));
            TableContinuationToken continuationToken = null;
            do
            {
                var result = await discordGuildSettings.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = result.ContinuationToken;
                foreach (var entity in result.Results)
                {
                    await discordGuildSettings.ExecuteAsync(TableOperation.Delete(entity));
                }
            } while (continuationToken != null);
        }

        public async Task<List<DiscordGuildSetting>> Get(string settingType, string guildId)
        {
            var query = new TableQuery<DiscordGuildSetting>().Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("Type", QueryComparisons.Equal, settingType), TableOperators.And, TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, guildId)));
            TableContinuationToken continuationToken = null;
            var worlds = new List<DiscordGuildSetting>();
            do
            {
                var result = await discordGuildSettings.ExecuteQuerySegmentedAsync(query, continuationToken);
                continuationToken = result.ContinuationToken;
                worlds.AddRange(result.Results);
            } while (continuationToken != null);

            return worlds;
        }
    }

    public class DiscordGuildSetting : TableEntity
    {
        public const string World = "world";
        public const string Guild = "guild";
        public const string MainWorld = "main-world";
        public DiscordGuildSetting(string guildId,string guildName, string type, string setting)
        {
            PartitionKey = guildId;
            RowKey = Guid.NewGuid().ToString("N");
            GuildName = guildName;
            Type = type;
            Value = setting;
        }

        public DiscordGuildSetting()
        {

        }

        public string Type { get; set; }
        public string GuildName { get; set; }
        public string Value { get; set; }
    }
}
