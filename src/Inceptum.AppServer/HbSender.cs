using System;
using System.Linq;
using Castle.Core;
using Castle.Core.Logging;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Monitoring;
using Inceptum.AppServer.Utils;
using Inceptum.Core.Utils;
using Inceptum.Messaging.Contract;

namespace Inceptum.AppServer
{
    internal class HbSender : IDisposable, IStartable
    {
        private const int DEFAULT_HB_INTERVAL = 3000;
        private readonly IHost m_Host;
        private readonly IMessagingEngine m_Engine;
    	private readonly Endpoint m_HbEndpoint;
        private PeriodicalBackgroundWorker m_PeriodicalBackgroundWorker;
        private readonly ILogger m_Logger;
        private readonly string m_InstanceName;
        private readonly int m_HbInterval;
        private IDisposable m_AppsChangeSubscription;

        public HbSender(IHost host, IMessagingEngine engine, Endpoint hbEndpoint, ILogger logger, string environment, int hbInterval)
        {
            m_HbInterval = hbInterval==0?DEFAULT_HB_INTERVAL:hbInterval;
            m_Logger = logger;
            m_Engine = engine;
        	m_HbEndpoint = hbEndpoint;
            m_Host = host;
            m_InstanceName = string.Format("{0}({1})", Environment.MachineName, environment);
        }


        private void sendHb(bool empty =false)
        {
            var hbMessage = new HostHbMessage
                                {
                                    Services = empty 
                                            ? new string[0]
                                            : m_Host.Instances.Where(a => a.Status == HostedAppStatus.Started).Select(a => a.ApplicationId).ToArray(),

                                    InstanceName = m_InstanceName,
                                    Period = m_HbInterval
                                };
            try
            {
				m_Engine.Send(hbMessage, m_HbEndpoint);
/*#if DEBUG
                m_Logger.DebugFormat("HeartBeat was sent");
#endif*/

            }catch(Exception e)
            {
                m_Logger.WarnFormat(e,"Failed to send hb");
            }
        }


        public void Start()
        {
            m_PeriodicalBackgroundWorker = new PeriodicalBackgroundWorker("Server HB sender", m_HbInterval, ()=>sendHb());
            m_AppsChangeSubscription = m_Host.AppsStateChanged.Subscribe(tuples =>
                                                                             {
                                                                                 m_Logger.DebugFormat("Sending HB as apps status changed");
                                                                                 sendHb();
                                                                             });
        }

        public void Stop()
        {
            m_AppsChangeSubscription.Dispose();
            m_AppsChangeSubscription = null;
            sendHb(true);

        }

        public void Dispose()
        {
            m_PeriodicalBackgroundWorker.Dispose();
        }
 
    }
}