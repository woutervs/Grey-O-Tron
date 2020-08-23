using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.Interfaces
{
    public interface IGw2DiscordUserRepository
    {
        Task InsertOrUpdate(Gw2DiscordUser gw2DiscordUser);

        Task<Gw2DiscordUser> Get(ulong userId);

        Task RemoveApiKey(ulong userId);

    }
}
