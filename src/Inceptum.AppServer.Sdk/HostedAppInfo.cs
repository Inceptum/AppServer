using System;

namespace Inceptum.AppServer
{
    [Serializable]
    public class HostedAppInfo
    {
         

        public HostedAppInfo(string name, string appType, string[] assembliesToLoad):
            this(name, appType, AppDomain.CurrentDomain.BaseDirectory,assembliesToLoad)
        {
        }

        public HostedAppInfo(string name, string appType, string baseDirectory, string[] assembliesToLoad)
        {
            AssembliesToLoad = assembliesToLoad;
            Name = name;
            AppType = appType;
            BaseDirectory = baseDirectory;
        }

        public string[] AssembliesToLoad { get; private set; }

        public string Name { get; set; }
        public string AppType { get; set; }
        public string BaseDirectory { get; set; }
        public string ConfigFile { get; set; }
        public string Version { get; set; }

        public string[] NativeDllToLoad { get; set; }
    }
}