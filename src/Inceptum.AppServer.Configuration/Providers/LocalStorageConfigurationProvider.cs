using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Inceptum.AppServer.Configuration.Json;
using Inceptum.AppServer.Configuration.Model;
using Inceptum.AppServer.Configuration.Persistence;

namespace Inceptum.AppServer.Configuration.Providers
{
    public class LocalStorageConfigurationProvider : IManageableConfigurationProvider
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

        #region IManageableConfigurationProvider Members

        public string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            string[] param = new[] {bundleName}.Concat(extraParams).ToArray();
            int paramLen = param.Length;

            Bundle bundle = null;
            //TODO: cache loaded configuratons
            Config config = getConfiguration(configuration);
            while (bundle == null && paramLen != 0)
            {
                string name = string.Join(".", param.Take(paramLen));
                bundle = config[name];
                paramLen--;
            }

            if (bundle == null)
                throw new BundleNotFoundException(String.Format("Bundle not found, configuration '{0}', bundle '{1}', params '{2}'", configuration, bundleName,
                                                                String.Join(",", extraParams ?? new string[0])));
            return bundle.Content;
        }

        public IEnumerable<object> GetAvailableConfigurations()
        {
            return m_Persister
                .GetAvailableConfigurations()
                .Select(getConfiguration)
                .Select(c => new
                                 {
                                     id = c.Name,
                                     name = c.Name,
                                     bundlesmap = c.Select(serializeBundle),
                                     bundles = c.Bundles.Select(
                                         bundle => new
                                                       {
                                                           id = bundle.Name,
                                                           name = bundle.ShortName,
                                                           configuration = c.Name,
                                                           content=bundle.Content,
                                                           purecontent=bundle.PureContent
                                                       }).ToArray()
                                 });
        }

        public IEnumerable<object> GetBundles(string configuration)
        {
            Config bundles = getConfiguration(configuration);
            return bundles.Select(serializeBundle).ToArray();
        }

        #endregion

        private Config getConfiguration(string configuration)
        {
            Config config;
            try
            {
                config = m_Persister.Load(configuration, m_ContentProcessor);
            }
            catch (Exception e)
            {
                throw new ConfigurationErrorsException(string.Format("Failed to load configuration '{0}'", configuration), e);
            }
            return config;
        }

        private static object serializeBundle(Bundle bundle)
        {
            return new { id = bundle.Name, name= bundle.ShortName , children = bundle.Select(serializeBundle).ToArray() };
            //return new {name = bundle.Name, content = bundle.PureContent, subbundles = bundle.Select(serializeBundle).ToArray()};
         //   return new {name = bundle.Name, content = bundle.PureContent, subbundles = bundle.Select(serializeBundle).ToArray()};
        }
    }
}