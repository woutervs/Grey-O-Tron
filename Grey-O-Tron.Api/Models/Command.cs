using GreyOTron.Library.Helpers;

namespace GreyOTron.Api.Models
{
    public class Command
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CommandOptions Options { get; set; }
        public string Arguments { get; set; }   
    }
}
