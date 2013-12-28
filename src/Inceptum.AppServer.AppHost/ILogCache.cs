using System.ServiceModel;

namespace Inceptum.AppServer.Logging
{
     [ServiceContract]
    public interface ILogCache
    {
          [OperationContract]
        void Add(LogEvent message);
    }
}