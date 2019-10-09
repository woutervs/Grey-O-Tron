using System;

namespace GreyOTron.Library.Exceptions
{
    public class RemoveRoleException : Exception
    {
        public string Role { get; }

        public RemoveRoleException(string role, Exception innerException) : base($"Could not remove role '{role}' from user.", innerException)
        {
            Role = role;
        }
    }
}
