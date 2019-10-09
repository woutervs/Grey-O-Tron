using System;
using System.Collections.Generic;

namespace GreyOTron.Library.Exceptions
{
    public class TooManyRequestsException : ApiInformationForUserByKeyException
    {
        public int SemaphoreCount { get; }
        public TooManyRequestsException(int semaphoreCount, string section, string key, string content, Exception innerException) : base(section, key, content, innerException)
        {
            SemaphoreCount = semaphoreCount;
        }

        public override Dictionary<string, string> AsDictionary()
        {
            
            var dict = base.AsDictionary();
            dict.Add("SemaphoreCount", SemaphoreCount.ToString());
            return dict;
        }
    }
}
