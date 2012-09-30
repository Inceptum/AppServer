using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Inceptum.AppServer.Model
{
    public class ApplicationInstanceInfo
    {
        public string Name { get; set; }
        public string ApplicationId { get; set; }

        [JsonConverter(typeof (StringVersionJsonConverter))]
        public Version Version { get; set; }

        [JsonConverter(typeof (StringEnumConverter))]
        public HostedAppStatus Status { get; set; }
    }
}