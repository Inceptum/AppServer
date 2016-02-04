using System.Collections.Generic;

namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationFacility
    {
        T DeserializeFromBundle<T>(string configuration, string bundleName, string jsonPath, IEnumerable<string> parameters);
    }
}