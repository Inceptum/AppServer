using System.Collections.Generic;

namespace Inceptum.AppServer.AppDiscovery
{
    public interface IApplicationBrowser
    {
        string Name { get;  }
        IEnumerable<HostedAppInfo> GetAvailabelApps();
    }
}