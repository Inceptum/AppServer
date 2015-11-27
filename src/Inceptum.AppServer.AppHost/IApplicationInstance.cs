using System.ServiceModel;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Hosting
{
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