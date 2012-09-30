using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using Inceptum.AppServer.Management2.OpenRasta;
using NLog;
using NLog.Targets;
using SignalR;
using SignalR.Hosting.Self;
using SignalR.Hubs;

namespace Inceptum.AppServer.Logging
{
    class SignalRhost
    {
       

        public static IDisposable Start()
         {

             string url = "http://*:8081/";
              url = "http://*:9223/sr/";
             var server = new Server(url);

             // Map the default hub url (/signalr)
             server.MapHubs();
             server.MapConnection<LogConnection>("/log");
             //SignalRPipelineContributor.MapConnection<LogConnection>("/log");
             // Start the server
             server.Start();
            
            return Disposable.Create(server.Stop);
         }
 
    }

    public class ManagementConsoleTarget: TargetWithLayout
    {
        private readonly ILogCache m_LogCache;
        private string m_Source;


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
