using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
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
