using System;
using System.Text;
using NLog;
using NLog.LayoutRenderers;

namespace Inceptum.AppServer.Utils
{
    /// <summary>
    /// Custom app domain renderer implementation to domain friendly name for logging
    /// </summary>
    [LayoutRenderer("appServer_app")]
    public class AppDomainDataRendererImpl : LayoutRenderer
    {
        /// <summary>
        /// Appends app domain friendly name to logging message
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="logEvent"></param>
        protected override void Append(StringBuilder builder, LogEventInfo logEvent)
        {
            var friendlyName = AppDomain.CurrentDomain.GetData("AppServer.Application")??"";
            builder.Append(friendlyName);
        }

 
    }
}