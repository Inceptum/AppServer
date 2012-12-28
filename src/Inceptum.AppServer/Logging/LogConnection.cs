using System.Collections.Generic;
using System.Threading.Tasks;
using SignalR;

namespace Inceptum.AppServer.Logging
{
    class LogConnection : PersistentConnection
    {
        private readonly LogCache m_LogCache;

        public LogConnection(LogCache logCache)
        {
            m_LogCache = logCache;
        }

 
        protected override Task OnConnectedAsync(IRequest request, string connectionId)
        {

            m_LogCache.RepeatFor(connectionId);
            return base.OnConnectedAsync(request, connectionId);
        }

        protected override Task OnReconnectedAsync(IRequest request, IEnumerable<string> groups, string connectionId)
        {
            m_LogCache.RepeatFor(connectionId);
            return base.OnReconnectedAsync(request, groups, connectionId);
        }
 
    }
}