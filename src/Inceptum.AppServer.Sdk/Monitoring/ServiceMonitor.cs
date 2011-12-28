using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Castle.Core;
using Inceptum.AppServer.Configuration;
using Inceptum.Core.Messaging;

namespace Inceptum.AppServer.Monitoring
{
    public class ServicesMonitor : IDisposable, IStartable
    {
        private IDisposable m_HandlerRegistration;
        private readonly Dictionary<string, InstanceInfo> m_Instances = new Dictionary<string, InstanceInfo>();
        private readonly IMessagingEngine m_MessagingEngine;
        private SonicEndpoint m_HbEndpoint;
        private volatile bool m_IsStarted;

        public ServicesMonitor(IMessagingEngine messagingEngine,SonicEndpoint hbEndpoint)
        {
            m_HbEndpoint = hbEndpoint;
            if (messagingEngine == null) throw new ArgumentNullException("messagingEngine");
            m_MessagingEngine = messagingEngine;
        }

        public Dictionary<string, InstanceInfo> Instances
        {
            get
            {
                lock (m_Instances)
                {
                    return m_Instances.ToDictionary(p => p.Key, p => p.Value);
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Stop();
        }

        #endregion

        private void processHb(HostHbMessage message)
        {
            InstanceInfo instance;
            lock (m_Instances)
            {
                if (!m_Instances.TryGetValue(message.InstanceName, out instance))
                {
                    instance = new InstanceInfo(message);
                    m_Instances.Add(message.InstanceName, instance);
                }
            }

            lock (instance)
            {
                instance.LastMessage = message;
            }
        }

        public void Start()
        {
            Thread.MemoryBarrier();
            m_HandlerRegistration = m_MessagingEngine.Subscribe<HostHbMessage>(m_HbEndpoint.Destination, m_HbEndpoint.TransportId, processHb);
            m_IsStarted = true;
            Thread.MemoryBarrier();
        }

        public void Stop()
        {
            Thread.MemoryBarrier();
            if (!m_IsStarted) return;
            
            m_IsStarted = false;
            Thread.MemoryBarrier();
            m_HandlerRegistration.Dispose();
        }
    }
}