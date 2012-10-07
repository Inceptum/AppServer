namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationProvider
    {
        string GetBundle(string configuration, string bundleName, params string[] extraParams);
    }

    public class ConfigurationInfo
    {
        public string Name { get; set; }
    }

    public class BundleInfo
    {
        public string id { get; set; } 
        public string Name { get; set; } 
        public string Configuration { get; set; } 
        public string Content { get; set; } 
    }
}
