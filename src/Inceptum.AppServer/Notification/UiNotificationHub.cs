using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.Utils;
using Inceptum.Core.Utils;
using SignalR;
using SignalR.Hubs;

namespace Inceptum.AppServer.Notification
{
    interface IHostNotificationListener
    {
        void InstancesChanged(string comment = null);
    }
    public class UiNotificationHub : Hub, IHostNotificationListener, IDisposable, IConnected
    {
        private const int HB_INTERVAL = 3000;
        private readonly PeriodicalBackgroundWorker m_Worker;

        public UiNotificationHub()
        {
            m_Worker = new PeriodicalBackgroundWorker("Server HB sender", HB_INTERVAL, HeartBeat);
        }

        public void HeartBeat()
        {
            if (Clients != null)
                Clients.HeartBeat();
        }


        public void InstancesChanged(string comment = null)
        {
            if (Clients!=null)
                Clients.InstancesChanged(comment);
        }



        public void Dispose()
        {
            m_Worker.Dispose();
        }

        public Task Connect()
        {
            return new Task(() => { });
        }

        public Task Reconnect(IEnumerable<string> groups)
        {
            return new Task(() => { });
        }
    }
}