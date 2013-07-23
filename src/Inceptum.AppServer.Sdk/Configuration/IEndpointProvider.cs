using Inceptum.Messaging.Contract;

namespace Inceptum.AppServer.Configuration
{
    public interface IEndpointProvider
    {
        bool Contains(string endpointName);
        Endpoint Get(string endpointName);
    }
}