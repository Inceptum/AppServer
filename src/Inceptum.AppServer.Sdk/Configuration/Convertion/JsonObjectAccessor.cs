using System;
using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.Configuration.Convertion
{
    internal class JsonObjectAccessor : IJsonObjectAccessor
    {
        public JToken SelectToken(string json, string path)
        {
            var token = JObject.Parse(json).SelectToken(path);

            return token;
        }

        public object ConvertTo(JToken token, Type to)
        {
            return token.ToObject(to);
        }
    }
}