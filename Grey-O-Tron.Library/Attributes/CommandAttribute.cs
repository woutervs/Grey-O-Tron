using System;
using System.ComponentModel.Composition;

namespace GreyOTron.Library.Attributes
{
    [MetadataAttribute]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; set; }
        public string CommandDescription { get; set; }
        public CommandOptions CommandOptions { get; set; }
        public string CommandArguments { get; set; }

        public CommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }

    [Flags]
    public enum CommandOptions
    {
        None = 0,
        DiscordServer = 1,
        DirectMessage = 2,
        RequiresAdmin = 4,
        RequiresOwner = 8
    }
}
