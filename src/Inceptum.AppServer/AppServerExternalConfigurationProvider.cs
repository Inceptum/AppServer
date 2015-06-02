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
            if (localProvider == null) throw new ArgumentNullException("localProvider");
            if (externalProvider == null) throw new ArgumentNullException("externalProvider");
            m_LocalStorageConfigurationProvider = localProvider;
            m_ExternalProvider = externalProvider;
        }

        public ConfigurationInfo[] GetConfigurations()
        {
            var appServerConfigurationinfo = m_LocalStorageConfigurationProvider.GetConfiguration("appserver");

            return m_ExternalProvider.GetConfigurations()
                                     .Where(c => !string.Equals(c.Name, "appserver", StringComparison.OrdinalIgnoreCase))
                                     .Concat(new[] { appServerConfigurationinfo })
                                     .ToArray();
        }

        private IManageableConfigurationProvider selectProvider(string configuration)
        {
            return string.Equals(configuration, "appserver", StringComparison.OrdinalIgnoreCase)
                       ? m_LocalStorageConfigurationProvider
                       : m_ExternalProvider;
        }

        public string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            return selectProvider(configuration).GetBundle(configuration, bundleName, extraParams);
        }

        public ConfigurationInfo GetConfiguration(string configuration)
        {
            return selectProvider(configuration).GetConfiguration(configuration);
        }

        public void DeleteBundle(string configuration, string bundle)
        {
            selectProvider(configuration).DeleteBundle(configuration, bundle);
        }

        public BundleInfo CreateOrUpdateBundle(string configuration, string name, string content)
        {
            return selectProvider(configuration).CreateOrUpdateBundle(configuration, name, content);
        }

        public void CreateConfiguration(string configuration)
        {
            selectProvider(configuration).CreateConfiguration(configuration);
        }

        public void DeleteConfiguration(string configuration)
        {
            selectProvider(configuration).DeleteConfiguration(configuration);
        }
    }
}