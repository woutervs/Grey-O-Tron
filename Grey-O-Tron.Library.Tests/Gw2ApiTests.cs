using GreyOTron.Library.ApiClients;
using GreyOTron.Library.Helpers;
using Xunit;

namespace Grey_O_Tron.Library.Tests
{
    public class Gw2ApiTests
    {
        [Fact]
        public void Test_Gw2Api()
        {
            var api = new Gw2Api(new CacheHelper());
            api.GetInformationForUserByKey("XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXXXXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX");
        }
    }
}
