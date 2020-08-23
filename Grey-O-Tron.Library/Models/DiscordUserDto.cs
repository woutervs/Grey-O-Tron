namespace GreyOTron.Library.Models
{
    public class DiscordUserDto
    {
        public ulong Id { get; set; }
        public string Username { get; set; }
        public string Discriminator { get; set; }
        public string PreferredLanguage { get; set; }
    }
}
