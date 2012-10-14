using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Inceptum.AppServer.Configuration.Json;
using Inceptum.AppServer.Configuration.Model;
using Inceptum.AppServer.Configuration.Persistence;
using OpenRasta.Web;

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

        public ConfigurationInfo[] GetConfigurations()
        {
            return m_Persister.GetAvailableConfigurations().Select(GetConfiguration).ToArray();
        }

        private BundleInfo[] getBundlesInfo(IEnumerable<Bundle> collection,string configuration)
        {
            return collection.Select(b => new BundleInfo(getBundlesInfo(b, configuration))
                                       {
                                           id = b.Name,
                                           Name = b.ShortName,
                                           Content = b.Content,
                                           Parent = string.Join(".",b.Name.Split(new[] {'.'}).Reverse().Skip(1).Reverse()),
                                           Configuration = configuration
                                       }).ToArray();
        }

        public ConfigurationInfo GetConfiguration(string configuration)
        {
            var config = getConfiguration(configuration);
            return new ConfigurationInfo(getBundlesInfo(config, configuration)){Name = configuration};
        }


        public string CreateConfiguration(string configuration)
        {
            return m_Persister.Create(configuration);
        }

        public bool DeleteConfiguration(string configuration)
        {
            return m_Persister.Delete(configuration);
        }

        public IEnumerable<BundleInfo> GetBundles(string configuration)
        {
            Config c = getConfiguration(configuration);
            return c.Bundles.Select(
                bundle => new BundleInfo
                              {
                                  id = bundle.Name,
                                  Name = bundle.ShortName,
                                  Content = bundle.Content,
                                  Configuration = c.Name
                              }
                ).ToArray();
        }

        public string GetBundle(string configuration, string bundleName, params string[] extraParams)
        {
            string[] param = new[] { bundleName }.Concat(extraParams).ToArray();
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
 
        public void CreateOrUpdateBundle(string configuration, string name, string content)
        {
            var config = getConfiguration(configuration);
            var parts = name.Split(new[] { '.' });

            BundleCollectionBase parentBundle = null;
            int i;
            for (i = parts.Length; i > 0 && parentBundle == null; i--)
            {
                var parent = string.Join(".", Enumerable.Range(0, i).Select(n => parts[n]));
                parentBundle = config.Bundles.FirstOrDefault(b => b.Name.ToLower() == parent.ToLower());
            }

            if (parentBundle == null)
            {
                parentBundle = config;
                for (int j = 0; j < parts.Length; j++)
                {
                    parentBundle = parentBundle.CreateBundle(parts[j]);
                }
            }
            else
            {
                for (int j = i + 1; j < parts.Length; j++)
                {
                    parentBundle = parentBundle.CreateBundle(parts[j]);
                }
            }          

            (parentBundle as Bundle).Content = content;
            m_Persister.Save(config);
        }

        public void DeleteBundle(string configuration, string name)
        {
            var config = getConfiguration(configuration);
            var bundle = config.Bundles.FirstOrDefault(b => b.Name.ToLower() == name.ToLower());
            if (bundle != null)
                bundle.Clear();
            m_Persister.Save(config);
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
            return new { id = bundle.Name, name = bundle.ShortName, children = bundle.Select(serializeBundle).ToArray() };
        }
    }
}