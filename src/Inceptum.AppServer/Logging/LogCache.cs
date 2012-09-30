using System.Collections.Generic;
using System.Linq;
using SignalR;

namespace Inceptum.AppServer.Logging
{
    public class LogCache : ILogCache
    {
        private readonly int m_Capacity;
        readonly List<LogEvent> m_Cache = new List<LogEvent>();

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