using System.Collections.Generic;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using System.ServiceModel;

namespace Inceptum.AppServer.Hosting
{
    [ServiceContract(CallbackContract = typeof(object))]
    internal interface IApplicationHost
    {
        HostedAppStatus Status { get; }
        InstanceCommand[] Start(IConfigurationProvider configurationProvider, ILogCache logCache, AppServerContext context, string instanceName, string environment);
        void Stop();
        string Execute(InstanceCommand command);
    }
}