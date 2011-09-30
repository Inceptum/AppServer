using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;

namespace Inceptum.AppServer.Configuration
{
    [ServiceContract]
    public interface IConfigurationService
    {
        [OperationContract]
        [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json,BodyStyle = WebMessageBodyStyle.Bare,UriTemplate = "{configuration}/{bundleName}/{*parameters}")]
        Message GetBundle(string configuration, string bundleName, string parameters);
    }
}
