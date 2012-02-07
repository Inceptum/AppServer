using System.Collections.Generic;
using Inceptum.AppServer.Configuration.Model;

namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationPersister
    {
        Config Load(string name, IContentProcessor contentProcessor);
        IEnumerable<string> GetAvailableConfigurations();
    }
}
