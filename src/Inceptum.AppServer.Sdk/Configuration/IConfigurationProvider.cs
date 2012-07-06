using System.Collections.Generic;

namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationProvider
    {
        string GetBundle(string configuration, string bundleName, params string[] extraParams);
    }

    public interface IManageableConfigurationProvider : IConfigurationProvider
    {
        IEnumerable<object> GetConfigurations();
        //void UpdateBundle(string configuration, string name, string content);
        //IEnumerable<object> GetBundles(string configuration);
        object GetConfiguration(string configuration);
        IEnumerable<BundleInfo> GetBundles(string configuration);
        void DeleteBundle(string configuration, string bundle);
        void CreateOrUpdateBundle(string configuration, string name, string content);
        string CreateConfiguration(string configuration);
        bool DeleteConfiguration(string configuration);
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
