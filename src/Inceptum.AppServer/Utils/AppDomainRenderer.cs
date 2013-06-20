using System;
using System.Text;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;

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
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("app_domain", typeof(AppDomainRendererImpl));
        }

    }

    /// <summary>
    /// Custom app domain renderer implementation to domain friendly name for logging
    /// </summary>
    [LayoutRenderer("app_domain")]
    public class AppDomainRendererImpl : LayoutRenderer
    {
        /// <summary>
        /// Appends app domain friendly name to logging message
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="logEvent"></param>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var friendlyName = AppDomain.CurrentDomain.FriendlyName;
            if (AppDomain.CurrentDomain.IsDefaultAppDomain() && DefaultAppDomainAlias != null)
                friendlyName = DefaultAppDomainAlias;
            builder.Append(friendlyName);
        }


        /// <summary>
        /// AppDomain FriendlyName
        /// </summary>
        [DefaultParameter]
        public string DefaultAppDomainAlias { get; set; }

    }
}
