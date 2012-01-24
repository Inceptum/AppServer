using OpenRasta.Configuration;
using OpenRasta.DI;
using OpenRasta.Hosting.HttpListener;

namespace Inceptum.AppServer.Management
{
    public class HttpListenerHostWithConfiguration : HttpListenerHost
    {
        private readonly IConfigurationSource m_Configuration;

        public HttpListenerHostWithConfiguration(IConfigurationSource configuration)
        {
            m_Configuration = configuration;
        }

        public override bool ConfigureRootDependencies(IDependencyResolver resolver)
        {
            bool result = base.ConfigureRootDependencies(resolver);
            if (result && m_Configuration != null)

                resolver.AddDependencyInstance<IConfigurationSource>(m_Configuration);
            return result;
        }
    }
}
