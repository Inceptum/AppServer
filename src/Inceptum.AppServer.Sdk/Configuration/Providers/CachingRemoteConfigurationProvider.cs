using System;
using System.IO;
using System.Linq;
using System.ServiceModel;
using Castle.Core.Logging;

namespace Inceptum.AppServer.Configuration.Providers
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class CachingRemoteConfigurationProvider : IManageableConfigurationProvider
    {
        private const string CONFIG_CACHE_PATH = "ConfigsCache";
        private readonly FileSystemConfigurationProvider m_FileSystemConfigurationProvider;
        private readonly IManageableConfigurationProvider m_ExternalProvider;
        private readonly ILogger m_Logger;

        public CachingRemoteConfigurationProvider(string serviceUrl)
            : this(serviceUrl, ".")
        {
        }

        public CachingRemoteConfigurationProvider(string serviceUrl, string path)
            : this(serviceUrl, path, NullLogger.Instance)
        {
        }

        public CachingRemoteConfigurationProvider(string serviceUrl, string path, ILogger logger)
            : this(new FileSystemConfigurationProvider(Path.Combine(Path.GetFullPath(path), CONFIG_CACHE_PATH)), new RemoteConfigurationProvider(serviceUrl), logger)
        {
        }

        protected internal CachingRemoteConfigurationProvider(FileSystemConfigurationProvider fileSystemConfigurationProvider, IManageableConfigurationProvider externalProvider, ILogger logger)
        {
            m_ExternalProvider = externalProvider;
            m_Logger = logger;
            m_FileSystemConfigurationProvider = fileSystemConfigurationProvider;
        }

        public virtual string  GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            string content;
            var provider = m_ExternalProvider;
            try
            {
                content = provider.GetBundle(configuration, bundleName, extraParams);
            }
            catch(Exception e)
            {
                m_Logger.WarnFormat(e,"Failed to retrieve bundle '{0}' with extra params {1} from remote source. Using cached value.", bundleName, string.Join(",", extraParams.Select(p => "'" + p + "'").ToArray()));
                content = null;
            }

            if (content == null)
            {
                try
                {
                    content = m_FileSystemConfigurationProvider.GetBundle(configuration,bundleName, extraParams.ToArray());
                }
                catch (Exception e)
                {
                    m_Logger.ErrorFormat(e,"Failed to retrieve bundle '{0}' with extra params {1} from cache.", bundleName, string.Join(",", extraParams.Select(p => "'" + p + "'").ToArray()));
                    return null;
                }

                if (content != null)
                {
                    m_Logger.InfoFormat("Bundle '{0}' with extra params {1}  was loaded from cache. Bundle Content:\r\n{2}", bundleName, string.Join(",", extraParams.Select(p => "'" + p + "'").ToArray()), content);
                }
                else
                    m_Logger.WarnFormat("Bundle '{0}' with extra params {1}  was not found in cache.", bundleName, string.Join(",", extraParams.Select(p => "'" + p + "'").ToArray()));
                return content;
            }

            m_Logger.InfoFormat("Bundle '{0}' with extra params {1}  was received from remote source. Bundle Content:\r\n{2}", bundleName, string.Join(",", extraParams.Select(p => "'" + p + "'").ToArray()), content);
            
            try
            {
                m_FileSystemConfigurationProvider.StoreBundle(configuration,bundleName, extraParams, content);
            }catch(Exception e)
            {
                m_Logger.WarnFormat(e,"Failed to persist bundle '{0}' with extra params {1}",bundleName,string.Join("," ,extraParams.Select(p=>"'"+p+"'").ToArray()));
            }

            return content;
        }

        protected virtual IManageableConfigurationProvider SelectProvider(string configuration)
        {
            if (configuration == null) throw new ArgumentNullException("configuration");
            return m_ExternalProvider;
        }

        public virtual ConfigurationInfo[] GetConfigurations()
        {
            return m_ExternalProvider.GetConfigurations();
        }

        public ConfigurationInfo GetConfiguration(string configuration)
        {
            return SelectProvider(configuration).GetConfiguration(configuration);
        }

        public void DeleteBundle(string configuration, string bundle)
        {
            SelectProvider(configuration).DeleteBundle(configuration, bundle);
        }

        public BundleInfo CreateOrUpdateBundle(string configuration, string name, string content)
        {
            return SelectProvider(configuration).CreateOrUpdateBundle(configuration, name, content);
        }

        public void CreateConfiguration(string configuration)
        {
            SelectProvider(configuration).CreateConfiguration(configuration);
        }

        public void DeleteConfiguration(string configuration)
        {
            SelectProvider(configuration).DeleteConfiguration(configuration);
        }
 
    }
}
