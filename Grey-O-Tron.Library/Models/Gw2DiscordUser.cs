using System;
using System.Collections.Generic;
using System.Text;

namespace GreyOTron.Library.Models
{
    public class Gw2DiscordUser
    {
        public DiscordUserDto DiscordUserDto { get; set; }
        public string ApiKey { get; set; }

        public string Gw2AccountId { get; set; }
    }
}
