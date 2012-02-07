using System.Collections.Generic;

namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationProvider
    {
        string GetBundle(string configuration, string bundleName, params string[] extraParams);
    }

    public interface IManageableConfigurationProvider : IConfigurationProvider
    {
        IEnumerable<object> GetAvailableConfigurations();
        IEnumerable<object> GetBundles(string configuration);
    }
}
