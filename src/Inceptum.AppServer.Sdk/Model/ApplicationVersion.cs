using System;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Model
{
    public class ApplicationVersion
    {
        public string Description { get; set; }
                
        [JsonConverter(typeof(StringVersionJsonConverter))]
        public Version Version { get; set; }

        [JsonIgnore]
        public string Browser { get; set; }
    }
}