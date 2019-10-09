using System;

namespace GreyOTron.Library.Exceptions
{
    public class InvalidKeyException : ApiInformationForUserByKeyException
    {
        public InvalidKeyException(string key, string section, string content, Exception innerException) : base(section, key, content, innerException)
        {
        }
    }
}
