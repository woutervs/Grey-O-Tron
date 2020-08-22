using System.Text;
using GreyOTron.Library.Helpers;

namespace GreyOTron.Library.Models
{
    public class Command
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public CommandOptions Options { get; set; }
        public string Arguments { get; set; }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append($"> **{Name}** ");
            if (!string.IsNullOrWhiteSpace(Arguments))
            {
                sb.Append($"*{Arguments}*");
            }
            sb.AppendLine();
            sb.AppendLine($"> \t{Description}");
            sb.AppendLine($"> \t{Options}");
            return sb.ToString();
        }
    }
}