using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Inceptum.AppServer.Model
{
    public class InstanceConfig
    {
        public string Name { get; set; }
        public string ApplicationId { get; set; }
        [JsonConverter(typeof(StringVersionJsonConverter))]
        public Version Version { get; set; }
        public bool AutoStart { get; set; }
    }
}