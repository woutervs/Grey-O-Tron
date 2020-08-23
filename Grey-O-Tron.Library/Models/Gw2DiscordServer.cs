using System.Collections.Generic;

namespace GreyOTron.Library.Models
{
    public class Gw2DiscordServer
    {
        public DiscordServerDto DiscordServer { get; set; }
        public Gw2WorldDto MainWorld { get; set; }
        public List<Gw2WorldDto> Worlds { get; set; }   
    }
}
