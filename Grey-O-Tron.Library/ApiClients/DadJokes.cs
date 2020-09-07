using System.Net.Http;
using System.Threading.Tasks;

namespace GreyOTron.Library.ApiClients
{
    public class DadJokes
    {
        private static readonly HttpClient Client = new HttpClient();
        
        public async Task<string> GetJoke()
        {
            Client.DefaultRequestHeaders.Clear();
            Client.DefaultRequestHeaders.Add("Accept", "text/plain");
            
            var result = await Client.GetStringAsync("https://icanhazdadjoke.com");
            
            return result;
        }
    }
}
