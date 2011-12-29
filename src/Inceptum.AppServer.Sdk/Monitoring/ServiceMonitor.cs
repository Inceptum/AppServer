using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Castle.Core;
using Inceptum.AppServer.Configuration;
using Inceptum.Core.Messaging;
using Inceptum.DataBus;
using Inceptum.DataBus.Messaging;

namespace Inceptum.AppServer.Monitoring
{
    /// <summary>
    /// 
    /// </summary>
    [Channel(ServicesMonitor.HB_CHANNEL)]
    public class HbFeedProvider:MessagingFeedProviderBase<HostHbMessage,EmptyContext>
    {
        private SonicEndpoint m_HbEndpoint;

        public HbFeedProvider(IMessagingEngine messagingEngine, SonicEndpoint hbEndpoint)
            : base(messagingEngine)
        {
            m_HbEndpoint = hbEndpoint;
        }

        protected override string GetSubscriptionSource(EmptyContext context)
        {
            return m_HbEndpoint.Destination;
        }

        protected override string GetSubscriptionTransportId(EmptyContext context)
        {
            return m_HbEndpoint.TransportId;
        }
    }


    [Channel(KNOWN_APPSEREVER_INSTANCES_CHANNEL)]
    public class ServicesMonitor : IStartable, IFeedProvider<InstanceInfo,EmptyContext>
    {
        public const string HB_CHANNEL = "HeartBeats";
        public const string KNOWN_APPSEREVER_INSTANCES_CHANNEL="KnownAppServerInstances";

        readonly Subject<InstanceInfo> m_InstancesSubject=new Subject<InstanceInfo>();
        private readonly Dictionary<string, InstanceInfo> m_Instances = new Dictionary<string, InstanceInfo>();

        [ImportChannel(HB_CHANNEL)]
        public IChannel<HostHbMessage> HbChannel { get; set; }

        public void Start()
        {
            HbChannel.Feed().Subscribe(processHb);
        }


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

        public void Stop()
        {
        }

        public bool CanProvideFor(EmptyContext context)
        {
            return true;
        }

        public IObservable<InstanceInfo> CreateFeed(EmptyContext context)
        {
            return m_InstancesSubject;
        }

        public IEnumerable<InstanceInfo> OnFeedLost(EmptyContext context)
        {
            return Enumerable.Empty<InstanceInfo>();
        }

        public IFeedResubscriptionPolicy GetResubscriptionPolicy(EmptyContext context)
        {
            return null;
        }
    }
}