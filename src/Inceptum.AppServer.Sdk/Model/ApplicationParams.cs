using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Castle.Core.Logging;

namespace Inceptum.AppServer.Model
{


    [DataContract]
    public class InstanceParams
    {
        private string m_LogLevel = "Debug";

       

        [DataMember]
        public string LogLevel
        {
            get { return m_LogLevel; }
            set { m_LogLevel = value; }
        }

        [DataMember]

        public AppServerContext AppServerContext { get; set; }
 

        [DataMember]
        public string Environment { get; set; }

        [DataMember]
        public Dictionary<string, string> AssembliesToLoad { get;  set; }

        [DataMember]
        public string DefaultConfiguration { get; set; }
         [DataMember]
        public long MaxLogSize { get; set; }
         [DataMember]
        public LogLimitReachedAction LogLimitReachedAction { get; set; }
    }
    public enum LogLimitReachedAction
    {
        LogToOversizedFolder = 0,
        StopLogging = 1
    }

    [DataContract]
    public class ApplicationParams
    {
        [DataMember]
        public string AppType { get; set; }
        [DataMember]
        public string[] ConfigFiles { get; set; }
        [DataMember]
        public string[] NativeDllToLoad { get; set; }
        [DataMember]
        public Dictionary<string, string> AssembliesToLoad { get; private set; }
        [DataMember]
        public bool Debug { get; set; }
       

        public ApplicationParams(string appType, string[] configFiles,   string[] nativeDllToLoad, Dictionary<string, string> assembliesToLoad)
        {
            AppType = appType;
            ConfigFiles = configFiles;
            NativeDllToLoad = nativeDllToLoad;
            AssembliesToLoad = assembliesToLoad.ToDictionary(p => p.Key, p => p.Value);
        }
        public ApplicationParams(string appType, string[] configFiles,  string[] nativeDllToLoad, Dictionary<AssemblyName, string> assembliesToLoad)
            : this(appType, configFiles,  nativeDllToLoad, assembliesToLoad.ToDictionary(p => p.Key.FullName, p => p.Value))
        {
         
        }

        protected bool Equals(ApplicationParams other)
        {
            return string.Equals(AppType, other.AppType) && string.Equals(ConfigFiles, other.ConfigFiles) && Equals(NativeDllToLoad, other.NativeDllToLoad) && Equals(AssembliesToLoad, other.AssembliesToLoad) && Debug.Equals(other.Debug);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ApplicationParams) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (AppType != null ? AppType.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (ConfigFiles != null ? ConfigFiles.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (NativeDllToLoad != null ? NativeDllToLoad.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (AssembliesToLoad != null ? AssembliesToLoad.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ Debug.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ApplicationParams left, ApplicationParams right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ApplicationParams left, ApplicationParams right)
        {
            return !Equals(left, right);
        }

        public ApplicationParams Clone()
        {
            return new ApplicationParams(AppType, ConfigFiles, NativeDllToLoad.ToArray(), new Dictionary<string, string>(AssembliesToLoad));
        }

        
    }
}