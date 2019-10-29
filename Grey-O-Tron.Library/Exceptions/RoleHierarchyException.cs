using System;

namespace GreyOTron.Library.Exceptions
{
    public class RoleHierarchyException : Exception
    {
        public RoleHierarchyException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }
}
