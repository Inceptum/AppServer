using System;
using System.Collections.Generic;
using System.Linq;
using Inceptum.AppServer.Configuration;
using Inceptum.Core.Messaging;

namespace Inceptum.AppServer.Monitoring
{
    public class ServicesMonitor : IDisposable
    {
        private readonly IDisposable m_HandlerRegistration;
        private readonly Dictionary<string, InstanceInfo> m_Instances = new Dictionary<string, InstanceInfo>();
        private readonly IMessagingEngine m_MessagingEngine;

        public ServicesMonitor(IMessagingEngine messagingEngine,SonicEndpoint hbEndpoint)
        {
            if (messagingEngine == null) throw new ArgumentNullException("messagingEngine");
            m_MessagingEngine = messagingEngine;

            m_HandlerRegistration = m_MessagingEngine.Subscribe<HostHbMessage>(hbEndpoint.Destination, hbEndpoint.TransportId, processHb);
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
            m_HandlerRegistration.Dispose();
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
    }
}