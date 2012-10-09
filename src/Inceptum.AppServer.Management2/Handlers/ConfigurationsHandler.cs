using System.Collections.Generic;
using System.Linq;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Model;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management2.Handlers
{
    public class ConfigurationsHandler
    {
        private readonly IManageableConfigurationProvider m_Provider;

        public ConfigurationsHandler(IManageableConfigurationProvider provider)
        {
            m_Provider = provider;
        }

        public object Get()
        {
            var configurations = m_Provider.GetConfigurations();

            return configurations.Select(c=>new {
                        id=c.Name,
                        bundles=getBundles(c,c.Name)}
                        ).ToArray();
        }   
        
        public object GetBundle(string configuration,string bundle)
        {
            var configurations = m_Provider.GetConfiguration(configuration);
            var b = configurations.Bundles.FirstOrDefault(x => x.Name == bundle);
            if (b == null)
                return new OperationResult.NotFound();

            return new
                       {
                           id = b.Name,
                           name = b.ShortName,
                           content=b.Content
                       };
        }

        private IEnumerable<object> getBundles(IEnumerable<Bundle> bundles, string configuration)
        {
            return bundles.Select(b => new
                                    {
                                        id = b.Name,
                                        name = b.ShortName,
                                        bundles = getBundles(b,configuration),
                                        configuration
                                    }).ToArray();
        }
    }
}