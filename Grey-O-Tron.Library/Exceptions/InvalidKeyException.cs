using System;

namespace GreyOTron.Library.Exceptions
{
    public class InvalidKeyException : ApiInformationForUserByKeyException
    {
        public InvalidKeyException(string section, string key, string content, Exception innerException) : base(section, key, content, innerException)
        {
        }
    }
}
