using System.Collections.Generic;

namespace Inceptum.AppServer
{
    class AppServerSetup
    {
        public AppServerSetup()
        {
            DebugFolders = new List<string>();
            DebugNativeDlls = new List<string>();
        }
        public string ConfSvcUrl { get; set; }
        public string[] AppsToStart { get; set; }
        public string Environment { get; set; }

        public List<string> DebugNativeDlls { get; set; }
        public List<string> DebugFolders { get; set; }
    }
}