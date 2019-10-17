using System;

namespace GreyOTron.Library.Exceptions
{
    public class EndpointRequiresAuthenticationException : ApiInformationForUserByKeyException
    {
        public EndpointRequiresAuthenticationException(string section, string key, string content, Exception innerException) : base(section, key, content, innerException)
        {
        }
    }
}
