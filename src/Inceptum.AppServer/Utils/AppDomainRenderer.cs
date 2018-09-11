using NLog.Config;

namespace Inceptum.AppServer.Utils
{
    /// <summary>
    /// Registers <see cref="AppDomainRendererImpl"/> as layout render
    /// </summary>
    public class AppDomainRenderer
    {
        /// <summary>
        /// Registers <see cref="AppDomainRendererImpl"/> as layout render
        /// </summary>
        public static void Register()
        {
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("app_domain", typeof (AppDomainRendererImpl));
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("appServer.instance", typeof(AppDomainDataRendererImpl));
        }

    }
}
