using System.Collections.Generic;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using System.ServiceModel;

namespace Inceptum.AppServer.Hosting
{
    [ServiceContract]
    internal interface IApplicationHost
    {
        HostedAppStatus Status { get; }
        InstanceCommand[] Start(IConfigurationProvider configurationProvider, ILogCache logCache, AppServerContext context, string instanceName, string environment);
        void Stop();
        string Execute(InstanceCommand command);
    }
    
    [ServiceContract]
    internal interface IApplicationHost2
    {
        [OperationContract]
        HostedAppStatus GetStatus();
        [OperationContract]
        InstanceCommand[] Start();
        [OperationContract]
        void Stop();
        [OperationContract]
        string Execute(InstanceCommand command);
    }
}