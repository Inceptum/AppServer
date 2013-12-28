using System.Collections.Generic;
using Inceptum.AppServer.Configuration.Model;

namespace Inceptum.AppServer.Configuration
{
    public interface IManageableConfigurationProvider : IConfigurationProvider
    {
        ConfigurationInfo[] GetConfigurations();
        ConfigurationInfo GetConfiguration(string configuration);
        void DeleteBundle(string configuration, string bundle);
        void CreateOrUpdateBundle(string configuration, string name, string content);
        void CreateConfiguration(string configuration);
        bool DeleteConfiguration(string configuration);
        BundleInfo GetBundleInfo(string configuration, string bundleName);
    }


    public class ConfigurationInfo
    {
        public string Name { get; set; }
        public BundleInfo[] Bundles { get; private set; }

        public ConfigurationInfo()
        {
        }

        public ConfigurationInfo(BundleInfo[] bundles)
        {
            Bundles = bundles;
        }
    }

    public class BundleInfo
    {
        public string id { get; set; }
        public string Parent { get; set; }
        public string Name { get; set; }
        public string Configuration { get; set; }
        public string Content { get; set; }
        public string PureContent { get; set; }
        public BundleInfo[] Bundles { get; private set; }

        public BundleInfo()
        {
        }

        public BundleInfo(BundleInfo[] bundles)
        {
            Bundles = bundles;
        }
    }
}