using System;
using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.Configuration.Convertion
{
    public interface IJsonObjectAccessor
    {
        JToken SelectToken(string json, string path);

        object ConvertTo(JToken token, Type to);
    }
}