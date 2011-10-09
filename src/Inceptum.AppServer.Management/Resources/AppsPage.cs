using Inceptum.AppServer.Management.Resources;

namespace Inceptum.AppServer.Management.Resources
{
    public class HostInfo
    {
        public string Name { get; set; }
        public string MachineName { get; set; }
        public Application[] Apps { get; set; }

    }

    public class Application
    {
        public string Name { get; set; }
        public bool IsStarted { get; set; }

    } 


    public class AppsPage : RootPage
    {
        public HostInfo[] Hosts { get; set; }
    }
}