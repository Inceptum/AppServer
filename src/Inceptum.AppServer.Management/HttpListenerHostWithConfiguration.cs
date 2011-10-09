using OpenRasta.Configuration;
using OpenRasta.DI;
using OpenRasta.Hosting.HttpListener;

namespace Inceptum.AppServer.Management
{
    public class HttpListenerHostWithConfiguration : HttpListenerHost
    {
        private readonly IConfigurationSource _configuration;

        public HttpListenerHostWithConfiguration(IConfigurationSource configuration)
        {
            _configuration = configuration;
        }

        public override bool ConfigureRootDependencies(IDependencyResolver resolver)
        {
            bool result = base.ConfigureRootDependencies(resolver);
            if (result && _configuration != null)

                resolver.AddDependencyInstance<IConfigurationSource>(_configuration);
            return result;
        }
    }
}