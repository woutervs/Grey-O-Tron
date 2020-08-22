using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.Interfaces
{
    public interface IDiscordUserRepository
    {
        Task InsertOrUpdate(DiscordUserDto discordUser);
        Task<DiscordUserDto> Get(ulong discordUserId);
    }
}
