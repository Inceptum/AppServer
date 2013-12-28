using System.ServiceModel;

namespace Inceptum.AppServer.Configuration
{
    [ServiceContract]
    public interface IConfigurationProvider
    {
        [OperationContract]
        string GetBundle(string configuration, string bundleName, params string[] extraParams);
    }


}
