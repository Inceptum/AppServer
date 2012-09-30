using System.Collections.Generic;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;

namespace Inceptum.AppServer.Hosting
{
    internal interface IApplicationHost
    {
        HostedAppStatus Status { get; }
        void Start(IConfigurationProvider configurationProvider, ILogCache logCache, AppServerContext context, string instanceName);
        void Stop();
    }
}