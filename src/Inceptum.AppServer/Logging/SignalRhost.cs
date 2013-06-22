using System;
using Castle.Core;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin.Hosting;
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