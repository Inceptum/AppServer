using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Castle.Core;
using Inceptum.AppServer.Management2.OpenRasta;
using NLog;
using NLog.Targets;
using SignalR;
using SignalR.Hosting.Self;
using SignalR.Hubs;

namespace Inceptum.AppServer.Logging
{
    class SignalRhost:IDisposable,IStartable
    {
        private readonly string m_Url;
        private Server m_Server;
        private readonly IDependencyResolver m_Resolver;

        public SignalRhost(int port,IDependencyResolver resolver)
        {
            m_Resolver = resolver;
            m_Url= string.Format("http://*:{0}/sr/", port);
        }

        public void Start()
        {
             m_Server = new Server(m_Url, m_Resolver);

             // Map the default hub url (/signalr)
             m_Server.MapHubs();
             m_Server.MapConnection<LogConnection>("/log");
             // Start the server
             m_Server.Start();
         }

        public void Stop()
        {
        }

        public void Dispose()
        {
            m_Server.Stop();
        }
    }

    public class ManagementConsoleTarget: TargetWithLayout
    {
        private readonly ILogCache m_LogCache;
        private readonly string m_Source;


        public ManagementConsoleTarget(ILogCache logCache,string source)
        {
            m_Source = source;
            m_LogCache = logCache;
        }

        protected override void Write(LogEventInfo logEvent) 
        { 
            string logMessage = Layout.Render(logEvent);
            m_LogCache.Add(new LogEvent { Level = logEvent.Level.Name, Message = logMessage, Source = m_Source }); 
        } 
    } 
}
