using System;
using System.ServiceModel;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
    //[ServiceContract(CallbackContract = typeof (IApplicationInitializer))]
    [ServiceContract]
    public interface IApplicationInstance
    {
        [OperationContract]
        void RegisterApplicationHost(string uri, InstanceCommand[] instanceCommands);
   
        [OperationContract]
        void ReportFailure(string error);
   
        [OperationContract]
        InstanceParams GetInstanceParams();
    }
}