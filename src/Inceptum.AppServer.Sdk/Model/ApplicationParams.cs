using System.Collections.Generic;
using System.Reflection;

namespace Inceptum.AppServer.Model
{
    public class ApplicationParams
    {
        public string AppType { get; set; }
        public string ConfigFile { get; set; }
        public string[] NativeDllToLoad { get; set; }
        public Dictionary<AssemblyName, string> AssembliesToLoad { get; private set; }

        public ApplicationParams(string appType, string configFile, string[] nativeDllToLoad, Dictionary<AssemblyName, string> assembliesToLoad)
        {
            AppType = appType;
            ConfigFile = configFile;
            NativeDllToLoad = nativeDllToLoad;
            AssembliesToLoad = new Dictionary<AssemblyName, string>(assembliesToLoad);
        }
    }
}