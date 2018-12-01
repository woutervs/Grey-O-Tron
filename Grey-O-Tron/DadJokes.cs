using System.Net.Http;
using System.Threading.Tasks;

namespace GreyOTron
{
    public class DadJokes
    {
        public static async Task<string> GetJoke()
        {
            using (var cli = new HttpClient())
            {
                cli.DefaultRequestHeaders.Clear();
                cli.DefaultRequestHeaders.Add("Accept", "text/plain");
                var result = await cli.GetStringAsync("https://icanhazdadjoke.com");
                return result;
            }
        }
    }
}
