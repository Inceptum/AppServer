using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceModel;
using System.Threading;
using Inceptum.AppServer.Configuration.Json;
using Inceptum.AppServer.Configuration.Model;
using Inceptum.AppServer.Configuration.Persistence;


namespace Inceptum.AppServer.Configuration.Providers
{
        
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class LocalStorageConfigurationProvider : IManageableConfigurationProvider
    {
        private readonly IContentProcessor m_ContentProcessor;
        private readonly IConfigurationPersister m_Persister;
        private readonly List<Config> m_Configurations;
        readonly ReaderWriterLockSlim m_ConfigurationsLock = new ReaderWriterLockSlim();


        public LocalStorageConfigurationProvider(string configFolder) :
            this(new FileSystemConfigurationPersister(configFolder), new JsonContentProcessor())
        {
        }

        public LocalStorageConfigurationProvider(IConfigurationPersister persister, IContentProcessor contentProcessor)
        {
            m_Persister = persister;
            m_ContentProcessor = contentProcessor;
            var query = from data in m_Persister.Load()
                        group data by data.Configuration
                            into config
                            select new Config(m_Persister,m_ContentProcessor, config.Key, config.ToDictionary(data => data.Name, data => data.Content));
            m_Configurations = new List<Config>(query.ToArray());
        }

        #region IManageableConfigurationProvider Members

        public ConfigurationInfo[] GetConfigurations()
        {
            m_ConfigurationsLock.EnterReadLock();
            try
            {
                return m_Configurations.Select(config => new ConfigurationInfo(getBundlesInfo(config, config.Name)) { Name = config.Name }).OrderBy(c => c.Name).ToArray();
            }
            finally
            {
                m_ConfigurationsLock.ExitReadLock();
            }
        }

        public BundleInfo GetBundleInfo(string configuration, string bundleName)
        {
            var config = findConfig(configuration);
            if (config == null)
            {
                throw new ConfigurationErrorsException(string.Format("Configuration {0} not found", configuration));
            }
            lock (config)
            {
                BundleInfo info = null;
                config.Visit(b =>
                    {
                        if (b.Name == bundleName)
                            info = new BundleInfo(getBundlesInfo(b, configuration))
                                {
                                    id = b.Name,
                                    Name = b.ShortName,
                                    Content = b.Content,
                                    PureContent = b.PureContent,
                                    Parent = b.Parent != null ? b.Parent.Name : null,
                                    Configuration = configuration
                                };
                    });
                return info;
            }
        }

        private BundleInfo[] getBundlesInfo(IEnumerable<Bundle> collection,string configuration)
        {
            return collection.Select(b => new BundleInfo(getBundlesInfo(b, configuration))
                                       {
                                           id = b.Name,
                                           Name = b.ShortName,
                                           Content = b.Content,
                                           PureContent = b.PureContent,
                                           Parent = b.Parent != null?b.Parent.Name:null,
                                           Configuration = configuration
                                       }).ToArray();
        }

        public ConfigurationInfo GetConfiguration(string configuration)
        {
            var config = findConfig(configuration);
            if (config == null)
            {
                throw new ConfigurationErrorsException(string.Format("Configuration {0} not found", configuration));
            }
            lock (config)
            {
                return new ConfigurationInfo(getBundlesInfo(config, config.Name)) { Name = config.Name };
            }
        }



        public void CreateConfiguration(string configuration)
        {
            m_ConfigurationsLock.EnterUpgradeableReadLock();
            try
            {
                Config config = m_Configurations.FirstOrDefault(c => c.Name == configuration);
                if (config != null)
                {
                    throw new InvalidOperationException(string.Format("Configuration with name {0} already exists",configuration));
                }

                config = new Config(m_Persister,m_ContentProcessor,configuration);
                m_ConfigurationsLock.EnterWriteLock();
                try
                {
                    m_Persister.Create(configuration);
                    m_Configurations.Add(config);
                }
                finally
                {
                    m_ConfigurationsLock.ExitWriteLock();
                }
            }
            finally
            {
                m_ConfigurationsLock.ExitUpgradeableReadLock();
            }
        }

        public void DeleteConfiguration(string configuration)
        {
            m_ConfigurationsLock.EnterUpgradeableReadLock();
            try
            {
                var config = m_Configurations.FirstOrDefault(c => c.Name == configuration);
                if (config == null)
                {
                    return;
                }
                m_ConfigurationsLock.EnterWriteLock();
                try
                {
                    m_Persister.Delete(configuration);
                    m_Configurations.Remove(config);
                }
                finally
                {
                    m_ConfigurationsLock.ExitWriteLock();
                }
            }
            finally
            {
                m_ConfigurationsLock.ExitUpgradeableReadLock();
            }
        }

        public string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            string[] param = new[] { bundleName }.Concat(extraParams).ToArray();
            int paramLen = param.Length;

            string bundleContent = null;

            var config = findConfig(configuration);
            while (bundleContent == null && paramLen != 0)
            {
                string name = string.Join(".", param.Take(paramLen));
                bundleContent = config[name];
                paramLen--;
            }

            if (bundleContent == null)
                throw new BundleNotFoundException(String.Format("Bundle not found, configuration '{0}', bundle '{1}', params '{2}'", configuration, bundleName,
                                                                String.Join(",", extraParams ?? new string[0])));
            return bundleContent;
        }

        private Config findConfig(string configuration)
        {
            Config config;
            m_ConfigurationsLock.EnterReadLock();
            try
            {
                config = m_Configurations.FirstOrDefault(c => c.Name == configuration.ToLower());
            }
            finally
            {
                m_ConfigurationsLock.ExitReadLock();
            }
            if (config == null)
            {
                throw new ConfigurationErrorsException(string.Format("Configuration {0} not found", configuration));
            }
            return config;
        }

 
        public BundleInfo CreateOrUpdateBundle(string configuration, string name, string content)
        {
            var config = findConfig(configuration);
            lock (config)
            {
                config[name]=content;
                config.Commit();
            }

            return GetBundleInfo(configuration,name);
            
        }

        public void DeleteBundle(string configuration, string name)
        {
            var config = findConfig(configuration);
            lock (config)
            {
                config.Delete(name);
                config.Commit();
            }
        }
        #endregion
    }
}