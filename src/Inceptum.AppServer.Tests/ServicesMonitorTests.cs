using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Castle.Facilities.Startable;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Monitoring;
using Inceptum.DataBus;
using Inceptum.DataBus.Castle;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.AppServer.Tests
{
    [TestFixture]
    public class ServicesMonitorTests
    {
        [Test]
        [Ignore("HB logic is corrupted and not used")]
        public void WaitForFirstMessageBeforeStartTest()
        {
            var hb=new Subject<HostHbMessage>();
            var rawHbProvider = MockRepository.GenerateMock<IFeedProvider<HostHbMessage, EmptyContext>>();
            rawHbProvider.Expect(provider => provider.CanProvideFor(EmptyContext.Value)).Return(true);
            rawHbProvider.Expect(provider => provider.CreateFeed(EmptyContext.Value)).Return(hb);
            rawHbProvider.Expect(provider => provider.GetResubscriptionPolicy(EmptyContext.Value)).Return(null);
            rawHbProvider.Expect(provider => provider.OnFeedLost(EmptyContext.Value)).Return(Enumerable.Empty<HostHbMessage>());

            var container=new WindsorContainer();
            container.AddFacility<ChannelRegistrationFacility>();
            (container.Resolve<IDataBus>() as DataBus.DataBus).RegisterFeedProvider("HeartBeats",rawHbProvider);

            container.Register(
                Component.For<IFeedProvider<InstanceInfo, EmptyContext>, ServicesMonitor>().ImplementedBy<ServicesMonitor>() 
                );
            var servicesMonitor = container.Resolve<ServicesMonitor>();
            var started=new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(state =>
                                             {
                                                 servicesMonitor.Start();
                                                 started.Set();
                                             });

            Thread.Sleep(200);
            Assert.That(started.WaitOne(0), Is.False,"ServicesMonitor started before first hb recieved and before timeout");
            hb.OnNext(new HostHbMessage(){InstanceName = "test",Period = 100,Services = new string[0]});
            Assert.That(started.WaitOne(500), Is.True, "ServicesMonitor has not started after first hb");
            
            


        }
         
    }
}