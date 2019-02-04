using GreyOTron.ApiClients;
using GreyOTron.Helpers;
using Xunit;

namespace Grey_O_Tron.Tests
{
    public class Gw2ApiTests
    {
        [Fact]
        public void FetchMatch()
        {
            var apiClient = new Gw2Api(new Cache());
            //apiClient.SetLinkedWorlds(new World {Id = 2003});
            
        }
    }
}
