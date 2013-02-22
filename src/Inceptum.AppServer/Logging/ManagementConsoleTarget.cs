using System.Collections.Generic;
using System.Reactive.Disposables;
using Inceptum.AppServer.Management.OpenRasta;
using NLog;
using NLog.Targets;
using SignalR;
using SignalR.Hubs;

namespace Inceptum.AppServer.Logging
{
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
