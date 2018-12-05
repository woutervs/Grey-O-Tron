using System;

namespace GreyOTron.CommandParser
{
    public class ArgumentProcessor
    {
        private readonly string prefix;

        public ArgumentProcessor(string prefix)
        {
            this.prefix = prefix;
        }

        public ICommand Parse(string argument)
        {
            argument = argument.Trim();
            if (!argument.StartsWith(prefix))
            {
                return new NullCommand();
            }

            var command = argument.Substring(prefix.Length, argument.Length - prefix.Length);
            var i = command.IndexOf(' ');
            if (i >= 0)
            {
                var j = i+1;
                argument = command.Substring(j, command.Length - j);
                command = command.Substring(0, i).ToLowerInvariant();
            }
            else
            {
                command = command.Trim().ToLowerInvariant();
            }

            return new NullCommand();
        }
    }
}
