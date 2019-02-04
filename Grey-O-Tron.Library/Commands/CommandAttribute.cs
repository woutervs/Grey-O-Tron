using System;
using System.ComponentModel.Composition;

namespace GreyOTron.Library.Commands
{
    [MetadataAttribute]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; set; }
        public string CommandDescription { get; set; }

        public CommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}
