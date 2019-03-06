using Newtonsoft.Json;
using RestSharp.Serializers;

namespace GreyOTron.Library.Helpers
{
    public class JsonConvertRestSharpSerializer : ISerializer
    {
        public JsonConvertRestSharpSerializer()
        {
            ContentType = "application/json";
        }

        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public string ContentType { get; set; }
    }
}
