using GreyOTron;
using Xunit;

namespace Grey_O_Tron.Tests
{
    public class ArgumentProcessorTests
    {
        [Fact]
        public void TestProcessor()
        {
            var processor = new CommandProcessor("got#", null);
            var result = processor.Parse("got#help");
            var result2 = processor.Parse("got#gw2-key EBJKQEJIO-EJITJWO-DJKEJEFI");

        }
    }
}
