using System.Collections.Generic;
using System.Linq;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Model;

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
                        bundles=getBundles(c)}
                        ).ToArray();
        }

        private IEnumerable<object> getBundles(IEnumerable<Bundle> bundles)
        {
            return bundles.Select(b => new
                                    {
                                        id = b.Name,
                                        name = b.ShortName,
                                        bundles = getBundles(b)
                                    }).ToArray();
        }
    }
}