using System;
using System.Linq;
using System.ServiceModel;
using Castle.Core.Logging;
using Inceptum.AppServer.Configuration;

namespace Inceptum.AppServer
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class AppServerExternalConfigurationProvider : IManageableConfigurationProvider
    {
        private readonly IManageableConfigurationProvider m_LocalStorageConfigurationProvider;
        private readonly IManageableConfigurationProvider m_ExternalProvider;

        public AppServerExternalConfigurationProvider(ILogger logger, IManageableConfigurationProvider localProvider, IManageableConfigurationProvider externalProvider) 
        {
            if (localProvider == null)
                throw new ArgumentNullException("localProvider");
            if (externalProvider == null) throw new ArgumentNullException("externalProvider");
            m_LocalStorageConfigurationProvider = localProvider;
            m_ExternalProvider = externalProvider;
        }

        public string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            if (configuration.ToLower() == "appserver")
                return m_LocalStorageConfigurationProvider.GetBundle(configuration, bundleName, extraParams);

            return m_ExternalProvider.GetBundle(configuration, bundleName, extraParams);
        }

        public ConfigurationInfo[] GetConfigurations()
        {
            var appServerConfigurationinfo = m_LocalStorageConfigurationProvider.GetConfiguration("appserver");

            return m_ExternalProvider.GetConfigurations()
                                     .Where(c => c.Name.ToLower() != "appserver")
                                     .Concat(new[] {appServerConfigurationinfo})
                                     .ToArray();
        }

        public ConfigurationInfo GetConfiguration(string configuration)
        {
            return m_ExternalProvider.GetConfiguration(configuration);
        }

        public void DeleteBundle(string configuration, string bundle)
        {
            m_ExternalProvider.DeleteBundle(configuration, bundle);
        }

        public BundleInfo CreateOrUpdateBundle(string configuration, string name, string content)
        {
            return m_ExternalProvider.CreateOrUpdateBundle(configuration, name, content);
        }

        public void CreateConfiguration(string configuration)
        {
            m_ExternalProvider.CreateConfiguration(configuration);
        }

        public void DeleteConfiguration(string configuration)
        {
            m_ExternalProvider.DeleteConfiguration(configuration);
        }
    }
}