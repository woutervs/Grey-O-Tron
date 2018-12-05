using System;
using System.ComponentModel.Composition;

namespace GreyOTron.Commands
{
    [MetadataAttribute]
    public class CommandAttribute : Attribute
    {
        public string CommandName { get; set; }

        public CommandAttribute(string commandName)
        {
            CommandName = commandName;
        }
    }
}
