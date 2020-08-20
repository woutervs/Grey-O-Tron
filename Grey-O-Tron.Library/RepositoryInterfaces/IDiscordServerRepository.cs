using System.Threading.Tasks;
using GreyOTron.Library.Models;

namespace GreyOTron.Library.RepositoryInterfaces
{
    public interface IDiscordServerRepository
    {
        Task InsertOrUpdate(DiscordServerDto discordServer);
    }
}
