using System;
using Newtonsoft.Json;

namespace Inceptum.AppServer.Logging
{
    [Serializable]
    public class LogEvent
    {
        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("level")]
        public string Level { get; set; }

        [JsonProperty("source")]
        public string Source { get; set; }
    }
}