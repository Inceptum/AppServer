using System.Collections.Generic;
using System.Diagnostics;

namespace Inceptum.AppServer.Configuration
{
    public interface IManageableConfigurationProvider : IConfigurationProvider
    {
        ConfigurationInfo[] GetConfigurations();
        ConfigurationInfo GetConfiguration(string configuration);
        void DeleteBundle(string configuration, string bundle);
        BundleInfo CreateOrUpdateBundle(string configuration, string name, string content);
        void CreateConfiguration(string configuration);
        void DeleteConfiguration(string configuration);
    }


    public class ConfigurationInfo
    {
        public string Name { get; set; }
        public BundleInfo[] Bundles { get;  set; }

        public ConfigurationInfo()
        {
        }

        public ConfigurationInfo(BundleInfo[] bundles)
        {
            Bundles = bundles;
        }
    }

    [DebuggerDisplay("id={id}, name={Name}, parent={Parent}, content={PureContent}")]
    public class BundleInfo
    {
        public string id { get; set; }
        public string Parent { get; set; }
        public string Name { get; set; }
        public string Configuration { get; set; }
        public string Content { get; set; }
        public string PureContent { get; set; }
        public BundleInfo[] Bundles { get;  set; }

        public BundleInfo()
        {
        }

        public BundleInfo(BundleInfo[] bundles)
        {
            Bundles = bundles;
        }
    }

    [DebuggerDisplay("id={id}, Configuration={Configuration}, Content={Content}")]
    public class BundleSearchResultItem
    {
        public string id { get; set; }
        public string Configuration { get; set; }
        public string Content { get; set; }
    }
}