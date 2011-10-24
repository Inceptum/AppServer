using Inceptum.AppServer.Configuration.Model;

namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationPersister
    {
        Config Load(string name, IContentProcessor contentProcessor);
    }
}
