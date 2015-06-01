using System;
using Newtonsoft.Json;
using NuGet;

namespace Inceptum.AppServer.Model
{
    public class ApplicationVersion
    {
        public string Description { get; set; }
                
        [JsonConverter(typeof(StringVersionJsonConverter))]
        public SemanticVersion Version { get; set; }

        [JsonIgnore]
        public string Browser { get; set; }
    }
}