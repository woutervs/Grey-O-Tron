using System;

namespace GreyOTron.Library.Exceptions
{
    public class RoleNotFoundException : Exception
    {
        public string Role { get; }

        public RoleNotFoundException(string role, Exception innerException) : base($"Tried to add a role '{role}' to the user that doesn't exist on the server anymore.", innerException)
        {
            Role = role;
        }
    }
}
