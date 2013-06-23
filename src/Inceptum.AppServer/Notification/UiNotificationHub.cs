using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Castle.Core.Logging;
using Inceptum.AppServer.Utils;
using Inceptum.Core.Utils;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;

namespace Inceptum.AppServer.Notification
{
    interface IHostNotificationListener
    {
        void InstancesChanged(string comment = null);
        void ApplicationsChanged(string comment = null);
    }

    public class UiNotifier : IHostNotificationListener,IDisposable
    {
         private const int HB_INTERVAL = 3000;
        private readonly PeriodicalBackgroundWorker m_Worker;

        public UiNotifier()
        {
            m_Worker = new PeriodicalBackgroundWorker("Server HB sender", HB_INTERVAL, HeartBeat);
        }

        public void HeartBeat()
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<UiNotificationHub>();
            context.Clients.All.HeartBeat();
        }
 
        public void InstancesChanged(string comment = null)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<UiNotificationHub>();
            context.Clients.All.InstancesChanged(comment);
        }

        public void ApplicationsChanged(string comment = null)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<UiNotificationHub>();
            context.Clients.All.ApplicationsChanged(comment);
        }


        public void Dispose()
        {
            m_Worker.Dispose();
        }
         
    }
    public class UiNotificationHub : Hub
    {
       
    }
}