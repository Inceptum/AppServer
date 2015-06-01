using System;
using System.Globalization;
using Newtonsoft.Json;
using NuGet;

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
                var version = (SemanticVersion)value;
                writer.WriteValue(version.ToString());
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            SemanticVersion version = null;
            if (reader.TokenType == JsonToken.String)
            {
                if (reader.Value == null || reader.Value.ToString() == string.Empty)
                    return null;
                if (!SemanticVersion.TryParse(reader.Value.ToString(), out version))
                    throw new Exception("Unexpected token when parsing version.");
            }else if (reader.TokenType != JsonToken.Null)
            {
                throw new Exception("Unexpected token when parsing version.");
            }
            return version;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(SemanticVersion);
        }


    }
}