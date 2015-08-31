using System.Collections.Generic;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using System.ServiceModel;
using NLog;

namespace Inceptum.AppServer.Hosting
{
   
    [ServiceContract]
    public interface IApplicationHost
    {
        [OperationContract]
        void Stop();
        [OperationContract]
        string Execute(InstanceCommand command);
        [OperationContract]
        void ChangeLogLevel(string level);
        [OperationContract]
        void Debug();
    }
}