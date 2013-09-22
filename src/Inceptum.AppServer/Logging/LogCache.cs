using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Castle.Core.Logging;
using Microsoft.AspNet.SignalR;
using NLog;
using NLog.Common;
using NLog.Targets;

namespace Inceptum.AppServer.Logging
{
        
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class LogCache : ILogCache
    {
        private readonly int m_Capacity;
        readonly List<LogEvent> m_Cache = new List<LogEvent>();
        private Logger m_Logger;

        private Logger Logger
        {
            get
            {
                if (m_Logger == null)
                    m_Logger = LogManager.GetLogger(GetType().FullName);
                return m_Logger;
            }
        }

        public LogCache(int capacity)
        {
            m_Capacity = capacity;
        }

        public void Add(LogEvent message)
        {
            lock (m_Cache)
            {
                m_Cache.Add(message);
                if (m_Cache.Count > m_Capacity)
                    m_Cache.RemoveRange(0, m_Cache.Count - m_Capacity);
            }
            if (message.Source != "Server")
            {
                var ev=new LogEventInfo(LogLevel.FromString(message.Level), Logger.Name, message.Message);
                ev.Properties["Source"] = message.Source;
          //      Logger.Log(LogLevel.FromString(message.Level), message.Message);
                Logger.Log(ev);
            }
            var context = GlobalHost.ConnectionManager.GetConnectionContext<LogConnection>();
            context.Connection.Broadcast(message);
        }

        public void RepeatFor(string conbnectionId)
        {
            LogEvent[] messages;
            lock(m_Cache)
            {
                messages = m_Cache.ToArray();
            }
            var context = GlobalHost.ConnectionManager.GetConnectionContext<LogConnection>();
            context.Connection.Send(conbnectionId,messages);
        }
    }
}