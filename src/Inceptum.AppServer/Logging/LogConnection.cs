using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;

namespace Inceptum.AppServer.Logging
{
    class LogConnection : PersistentConnection
    {
        private readonly LogCache m_LogCache;

        public LogConnection(LogCache logCache)
        {
            m_LogCache = logCache;
        }

 
        protected override Task OnConnected(IRequest request, string connectionId)
        {

            m_LogCache.RepeatFor(connectionId);
            return base.OnConnected(request, connectionId);
        }


        protected override Task OnReconnected(IRequest request, string connectionId)
        {
            m_LogCache.RepeatFor(connectionId);
            return base.OnReconnected(request, connectionId);
        }
 
    }
}