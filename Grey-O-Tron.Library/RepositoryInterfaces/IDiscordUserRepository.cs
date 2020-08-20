using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.RepositoryInterfaces
{
    public interface IDiscordUserRepository
    {
        Task InsertOrUpdate(DiscordUserDto discordUser);
    }
}
