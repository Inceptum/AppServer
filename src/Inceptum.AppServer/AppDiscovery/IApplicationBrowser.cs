using System.Collections.Generic;

namespace Inceptum.AppServer.AppDiscovery
{
    public interface IApplicationBrowser
    {
        IEnumerable<HostedAppInfo> GetAvailabelApps();
    }
}