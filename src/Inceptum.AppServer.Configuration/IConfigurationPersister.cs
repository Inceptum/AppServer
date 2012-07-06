using System.Collections.Generic;
using Inceptum.AppServer.Configuration.Model;

namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationPersister
    {
        Config Load(string name, IContentProcessor contentProcessor);
        IEnumerable<string> GetAvailableConfigurations();
        void Save(Config config);
        string Create(string name);
        bool Delete(string name);
    }
}
