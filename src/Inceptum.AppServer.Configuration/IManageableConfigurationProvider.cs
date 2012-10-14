using System.Collections.Generic;
using Inceptum.AppServer.Configuration.Model;

namespace Inceptum.AppServer.Configuration
{
    public interface IManageableConfigurationProvider : IConfigurationProvider
    {
        ConfigurationInfo[] GetConfigurations();
        ConfigurationInfo GetConfiguration(string configuration);
        IEnumerable<BundleInfo> GetBundles(string configuration);
        void DeleteBundle(string configuration, string bundle);
        void CreateOrUpdateBundle(string configuration, string name, string content);
        string CreateConfiguration(string configuration);
        bool DeleteConfiguration(string configuration);
    }
}