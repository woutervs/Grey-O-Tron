using System;
using System.Collections.Generic;

namespace GreyOTron.Library.Exceptions
{
    public class ApiInformationForUserByKeyException : Exception
    {
        public string Section { get; }
        public string Key { get; }
        public string Content { get; }



        public ApiInformationForUserByKeyException(string section, string key, string content, Exception innerException) : base(innerException.Message, innerException)
        {
            Section = section;
            Key = key;
            Content = content;
        }

        public virtual Dictionary<string, string> AsDictionary() =>
            new Dictionary<string, string>
            {
                { "Section", Section},
                { "Key", Key },
                { "Content", Content }
            };
    }
}
