using System.Collections.Generic;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer.Hosting
{
    internal interface IApplicationHost
    {
        HostedAppStatus Status { get; }
        HostedAppInfo AppInfo { get; }
        void Start(IConfigurationProvider configurationProvider, AppServerContext context);
        void Stop();
    }
}