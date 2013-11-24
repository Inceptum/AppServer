using System;
using Castle.Core;
using Castle.Core.Logging;
using Inceptum.AppServer.Configuration;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json.Linq;
using Owin;

namespace Inceptum.AppServer.Logging
{
    class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Turn cross domain on 
            var config = new HubConfiguration { EnableCrossDomain = true, Resolver = GlobalHost.DependencyResolver,EnableJavaScriptProxies = true};
            
            // This will map out to http://localhost:8080/signalr by default
            app.MapHubs(config).MapConnection<LogConnection>("/log"); 
            
        }
    }

    class SignalRhost:IDisposable,IStartable
    {
        private readonly string m_Url;
        private IDisposable m_Server;
        private ILogger m_Logger;


        public SignalRhost(ILogger logger,IManageableConfigurationProvider configurationProvider)
        {
            m_Logger = logger;
            int port = 9223;
            try
            {
                var bundleString = configurationProvider.GetBundle("AppServer", "server.host", "{environment}", "{machineName}");
                dynamic bundle = JObject.Parse(bundleString).SelectToken("ManagementConsole");
                port = bundle.port;
            }
            catch (Exception e)
            {
                m_Logger.WarnFormat(e, "Failed to get port from configuration , using default 9223");
            }
            m_Url = string.Format("http://*:{0}/sr/", port);
        }

        public SignalRhost(int port)
        {
            m_Url= string.Format("http://*:{0}/sr/", port);
        }

        public void Start()
        {
            m_Server = WebApplication.Start<Startup>(m_Url);
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
            m_Server.Dispose();
        }
    }
}