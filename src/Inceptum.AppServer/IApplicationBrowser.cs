using System;
using System.Collections.Generic;

namespace Inceptum.AppServer
{
    public interface IApplicationBrowser
    {
        IEnumerable<AppInfo> GetAvailabelApps();
        HostedAppInfo GetAppLoadParams(AppInfo appInfo);
    }
}