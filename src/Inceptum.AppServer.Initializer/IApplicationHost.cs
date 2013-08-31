using System.Collections.Generic;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using System.ServiceModel;

namespace Inceptum.AppServer.Hosting
{
   
    [ServiceContract]
    public interface IApplicationHost
    {
        [OperationContract]
        InstanceCommand[] Start();
        [OperationContract]
        void Stop();
        [OperationContract]
        string Execute(InstanceCommand command);
    }
}