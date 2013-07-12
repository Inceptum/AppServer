using System.Collections.Generic;

namespace Inceptum.AppServer
{
    class AppServerSetup
    {
        public AppServerSetup()
        {
            DebugFolders = new List<string>();
        }
        public string ConfSvcUrl { get; set; }
        public string[] AppsToStart { get; set; }
        public string Environment { get; set; }
        public string Repository { get; set; }
        public bool SendHb { get; set; }
        public int HbInterval { get; set; }
        public string[] DebugWraps { get; set; }

        public List<string> DebugFolders { get; set; }
    }
}