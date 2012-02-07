using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
    public class ServerCore : IServerCore
    {
        public ServerCore(IManageableConfigurationProvider localStorageConfigurationProvider)
        {
            LocalConfigurationProvider = localStorageConfigurationProvider;
        }

        public IManageableConfigurationProvider LocalConfigurationProvider { get; private set; }
    }
}