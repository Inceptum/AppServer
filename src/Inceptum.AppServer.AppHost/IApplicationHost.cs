using System.ServiceModel;

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