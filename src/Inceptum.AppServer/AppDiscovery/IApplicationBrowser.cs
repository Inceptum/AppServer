using System;
using System.Collections.Generic;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.AppDiscovery
{
    public interface IApplicationBrowser
    {
        string Name { get;  }
        IEnumerable<HostedAppInfo> GetAvailableApps();
        ApplicationParams GetApplicationParams(string application, Version version);
    }
}