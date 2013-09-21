using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Inceptum.AppServer.Model
{
    public class InstanceConfig
    {
        private string m_User;
        public string Name { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationVendor { get; set; }
        [JsonConverter(typeof(StringVersionJsonConverter))]
        public Version Version { get; set; }
        public bool AutoStart { get; set; }

        public string User
        {
            get { return string.IsNullOrWhiteSpace(m_User) ? "" : m_User; }
            set { m_User = value; }
        }

        public string Password { get; set; }
        public string Environment { get; set; }
    }
}