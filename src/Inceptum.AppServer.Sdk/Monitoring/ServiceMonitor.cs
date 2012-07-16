using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Castle.Core;
using Inceptum.AppServer.Configuration;
using Inceptum.DataBus;

namespace Inceptum.AppServer.Monitoring
{
	[Channel(KNOWN_APPSEREVER_INSTANCES_CHANNEL)]
    public class ServicesMonitor : IStartable, IFeedProvider<InstanceInfo, EmptyContext>
    {
        public const string HB_CHANNEL = "HeartBeats";
        public const string KNOWN_APPSEREVER_INSTANCES_CHANNEL="KnownAppServerInstances";

        readonly Subject<InstanceInfo> m_InstancesSubject=new Subject<InstanceInfo>();
        private readonly Dictionary<string, InstanceInfo> m_Instances = new Dictionary<string, InstanceInfo>();

        [ImportChannel(HB_CHANNEL)]
        public IChannel<HostHbMessage> HbChannel { get; set; }

        public void Start()
        {
            var feed = HbChannel.Feed();
            var gotFirstMessage=new ManualResetEvent(false);
            var firstMessageSubscription = feed.Take(1).Subscribe(message => gotFirstMessage.Set());
            feed.Subscribe(processHb);
            gotFirstMessage.WaitOne(15000);
            firstMessageSubscription.Dispose();
        }


        public bool HasAtLeastOneInstanceOfService(string serviceName)
        {
            return Instances.Where(i => i.Value.Alive && i.Value.Servcies != null && i.Value.Servcies.Contains(serviceName)).Any();
        }

        private void processHb(HostHbMessage message)
        {
            InstanceInfo instance;
            bool newInstanceDiscovered;
            lock (m_Instances)
            {
                newInstanceDiscovered = !m_Instances.TryGetValue(message.InstanceName, out instance);
                if (newInstanceDiscovered)
                {
                    instance = new InstanceInfo(message);
                    m_Instances.Add(message.InstanceName, instance);
                }

                instance.LastMessage = message;
            }

            if (newInstanceDiscovered)
                m_InstancesSubject.OnNext(instance);
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