namespace Inceptum.AppServer
{
    class AppServerSetup
    {
        public string ConfSvcUrl { get; set; }
        public string[] AppsToStart { get; set; }
        public string Environment { get; set; }
        public string Repository { get; set; }
        public bool SendHb { get; set; }
        public int HbInterval { get; set; }
        public string[] DebugWraps { get; set; }
    }
}