using System;

namespace GreyOTron.Library.Exceptions
{
    public class ApiTimeoutException : ApiInformationForUserByKeyException
    {
        public ApiTimeoutException(string section, string key, string content, Exception innerException) : base(section, key, content, innerException)
        {
        }
    }
}
