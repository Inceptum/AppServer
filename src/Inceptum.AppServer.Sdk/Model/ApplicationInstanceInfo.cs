using System;
using Inceptum.AppServer.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Inceptum.AppServer.Model
{
    public class ApplicationInstanceInfo
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationVendor { get; set; }

        [JsonConverter(typeof (StringVersionJsonConverter))]
        public Version Version { get; set; }

        [JsonConverter(typeof (StringEnumConverter))]
        public HostedAppStatus Status { get; set; }

        public bool AutoStart { get; set; }

        [JsonConverter(typeof(StringVersionJsonConverter))]
        public Version ActualVersion { get; set; }

        public string Environment { get; set; }

        public InstanceCommand[] Commands { get; set; }
    }
}