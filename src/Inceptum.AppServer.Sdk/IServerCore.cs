using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
    public interface IServerCore
    {
        IManageableConfigurationProvider LocalConfigurationProvider { get; }
    }
}