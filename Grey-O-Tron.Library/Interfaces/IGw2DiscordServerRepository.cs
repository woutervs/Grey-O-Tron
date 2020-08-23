using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.Interfaces
{
    public interface IGw2DiscordServerRepository
    {
        Task InsertOrUpdate(Gw2DiscordServer gw2DiscordServer);
        Task<Gw2DiscordServer> Get(ulong discordId);
    }
}
