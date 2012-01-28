using System;
using System.Configuration;
using System.Linq;
using Inceptum.AppServer.Configuration.Json;
using Inceptum.AppServer.Configuration.Model;
using Inceptum.AppServer.Configuration.Persistence;

namespace Inceptum.AppServer.Configuration.Providers
{
    public class LocalStorageConfigurationProvider :  IConfigurationProvider
    {
        private readonly IContentProcessor m_ContentProcessor;
        private readonly IConfigurationPersister m_Persister;


        public LocalStorageConfigurationProvider(string configFolder) :
            this(new FileSystemConfigurationPersister(configFolder), new JsonContentProcessor())
        {
        }

        public LocalStorageConfigurationProvider(IConfigurationPersister persister, IContentProcessor contentProcessor)
        {
            m_Persister = persister;
            m_ContentProcessor = contentProcessor;
        }

        #region IConfigurationProvider Members

        public string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            var param = new[] {bundleName}.Concat(extraParams);
            int paramLen = param.Count();

            Bundle bundle = null;

            Config config;
            try
            {
                config = m_Persister.Load(configuration, m_ContentProcessor);
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(string.Format("Failed to load configuration '{0}'", configuration),e);
            }
            while (bundle == null && paramLen != 0)
            {
                string name = string.Join(".", param.Take(paramLen));
                bundle = config[name];
                paramLen--;
            }

            if (bundle == null)
                throw new BundleNotFoundException("Bundle not found");
            return bundle.Content;
        }

        #endregion

    }
}