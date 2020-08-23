using System.Net.Http;
using System.Threading.Tasks;

namespace GreyOTron.Library.ApiClients
{
    public class DadJokes
    {
        public async Task<string> GetJoke()
        {
            using var cli = new HttpClient();
            cli.DefaultRequestHeaders.Clear();
            cli.DefaultRequestHeaders.Add("Accept", "text/plain");
            var result = await cli.GetStringAsync("https://icanhazdadjoke.com");
            return result;
        }
    }
}
