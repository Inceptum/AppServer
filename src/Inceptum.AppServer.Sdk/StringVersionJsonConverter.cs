using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Inceptum.AppServer
{
    public class StringVersionJsonConverter : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else
            {
                var version= (Version)value;
                writer.WriteValue(version.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            Version version=null;
            if (reader.TokenType == JsonToken.String)
            {
                if (reader.Value == null || reader.Value.ToString() == string.Empty)
                    return null;
                if(!Version.TryParse(reader.Value.ToString(),out version))
                    throw new Exception("Unexpected token when parsing version. Expected String in version format (\\d+.\\d+.\\d+.\\d+)");
            }else if (reader.TokenType != JsonToken.Null)
            {
                throw new Exception("Unexpected token when parsing version. Expected String in version format (\\d+.\\d+.\\d+.\\d+)");
            }
            return version;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType==typeof(Version);
        }


    }
}