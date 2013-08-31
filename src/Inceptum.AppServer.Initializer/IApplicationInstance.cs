using System;
using System.ServiceModel;
using Inceptum.AppServer.Initializer;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
    //[ServiceContract(CallbackContract = typeof (IApplicationInitializer))]
    [ServiceContract]
    public interface IApplicationInstance
    {
        [OperationContract]
        void RegisterApplicationHost(string uri);
   
        [OperationContract]
        InstanceParams GetInstanceParams();
    }
}