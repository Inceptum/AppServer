using System;
using System.Collections.Generic;
using System.Linq;

namespace Inceptum.AppServer
{
    [Serializable]
    public class HostedAppInfo
    {
         

        public HostedAppInfo(string name,  Version version,string appType, string[] assembliesToLoad):
            this(name,version, appType, AppDomain.CurrentDomain.BaseDirectory,assembliesToLoad)
        {
        }

        public HostedAppInfo(string name, Version version,string appType, string baseDirectory, IEnumerable<string> assembliesToLoad, IEnumerable<string> nativeDllToLoad = null)
        {
            AssembliesToLoad = assembliesToLoad.ToArray();
            NativeDllToLoad = (nativeDllToLoad??new string[0]).ToArray();
            Name = name;
            AppType = appType;
            BaseDirectory = baseDirectory;
            Version = version;
        }

        public string[] AssembliesToLoad { get; private set; }

        public string Name { get; set; }
        public string AppType { get; set; }
        public string BaseDirectory { get; set; }
        public string ConfigFile { get; set; }
        public Version Version { get; set; }

        public string[] NativeDllToLoad { get; private set; }

        public override string ToString()
        {
            return Name + " v" + Version;
        }
    }
}