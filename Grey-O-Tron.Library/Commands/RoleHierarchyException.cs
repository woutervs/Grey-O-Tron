using System;

namespace GreyOTron.Library.Commands
{
    public class RoleHierarchyException : Exception
    {
        public RoleHierarchyException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
