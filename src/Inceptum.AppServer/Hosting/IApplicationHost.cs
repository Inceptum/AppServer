using System.Collections.Generic;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;

namespace Inceptum.AppServer.Hosting
{
    internal interface IApplicationHost
    {
        HostedAppStatus Status { get; }
        InstanceCommandSpec[] Start(IConfigurationProvider configurationProvider, ILogCache logCache, AppServerContext context, string instanceName, string environment);
        void Stop();
        string Execute(string command);
    }
}