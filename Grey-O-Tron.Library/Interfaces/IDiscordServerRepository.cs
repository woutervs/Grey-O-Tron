using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.Interfaces
{
    public interface IDiscordServerRepository
    {
        Task InsertOrUpdate(DiscordServerDto discordServer);
        Task<DiscordServerDto> Get(ulong discordServerId);
    }
}
