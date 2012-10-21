namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationProvider
    {
        string GetBundle(string configuration, string bundleName, params string[] extraParams);
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
