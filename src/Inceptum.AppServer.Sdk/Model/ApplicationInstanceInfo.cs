﻿using System;
using Castle.Core.Logging;
using Inceptum.AppServer.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NuGet;

namespace Inceptum.AppServer.Model
{
    public class ApplicationInstanceInfo
    {
        private LoggerLevel m_LogLevel=LoggerLevel.Debug;
        public string Name { get; set; }
        public string Id { get; set; }
        public string ApplicationId { get; set; }
        public string ApplicationVendor { get; set; }

        [JsonConverter(typeof (StringVersionJsonConverter))]
        public SemanticVersion Version { get; set; }

        [JsonConverter(typeof (StringEnumConverter))]
        public HostedAppStatus Status { get; set; }

        public bool AutoStart { get; set; }

        public bool IsDebuggable { get; set; }

        [JsonConverter(typeof(StringVersionJsonConverter))]
        public SemanticVersion ActualVersion { get; set; }

        public string Environment { get; set; }

        public string DefaultConfiguration { get; set; }
        
        public long MaxLogSize { get; set; }
                
        [JsonConverter(typeof(StringEnumConverter))]
        public LogLimitReachedAction LogLimitReachedAction { get; set; }

        public InstanceCommand[] Commands { get; set; }

        public string User { get; set; }

        public string EffectiveUser
        {
            get
            {
                var serverIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
                string serverUserName = null;
                if (serverIdentity != null)
                    serverUserName = serverIdentity.Name;
                return string.IsNullOrEmpty(User) ? serverUserName : User;
            }
        }

        public string Password { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public LoggerLevel LogLevel
        {
            get { return m_LogLevel; }
            set { m_LogLevel = value; }
        }
    }

   
}