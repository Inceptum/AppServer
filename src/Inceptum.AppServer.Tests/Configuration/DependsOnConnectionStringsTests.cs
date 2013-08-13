using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using NUnit.Framework;

namespace Inceptum.AppServer.Tests.Configuration
{
    [TestFixture]
    public class DependsOnConnectionStringsTests
    {
        private ConnectionString m_ConnectionString1;
        private ConnectionString m_ConnectionString2;
        private ConnectionString m_ConnectionString3;
        private ConnectionString m_ConnectionString4;
        private ConnectionString m_ConnectionString5;
        private WindsorContainer m_Container; 
        [SetUp]
        public void SetUp()
        {
            m_ConnectionString1 = new ConnectionString("c1");
            m_ConnectionString2 = new ConnectionString("c2");
            m_ConnectionString3 = new ConnectionString("c3");
            m_ConnectionString4 = new ConnectionString("c4");
            m_ConnectionString5 = new ConnectionString("c5");
            var endpointResolver = new ConnectionStringResolver(new Dictionary<string, string>()
		        {
		            {"cs1", m_ConnectionString1},
		            {"cs2", m_ConnectionString2},
		            {"cs3", m_ConnectionString3},
		            {"cs4", m_ConnectionString4},
		            {"cs5", m_ConnectionString5},
		        });

            m_Container = new WindsorContainer();
            m_Container.Kernel.Resolver.AddSubResolver(endpointResolver);
        }

        [Test]
        public void EndpointResolveByConstructorParameterNameTest()
        {
            m_Container.Register(Component.For<ConnectionStringsTest1>());

            var test1 = m_Container.Resolve<ConnectionStringsTest1>();
            Assert.AreEqual(m_ConnectionString1, test1.ConnectionString);
            Assert.AreEqual(m_ConnectionString1, test1.ConnectionString);
        }
        [Test]
        public void EndpointResolveByOverridenParameterNameTest()
        {
            m_Container.Register(Component.For<ConnectionStringsTest1>().WithConnectionStrings(new { cs1 = "cs2" }));

            var test1 = m_Container.Resolve<ConnectionStringsTest1>();
            Assert.AreEqual(m_ConnectionString2, test1.ConnectionString);
        }

        [Test]
        public void EndpointResolveByExplicitEndpointParameterNameTest()
        {
            var connectionString = new ConnectionString("custom-connection-string");

            m_Container.Register(Component.For<ConnectionStringsTest1>().WithConnectionStrings(new { cs1 = connectionString }));
            var test1 = m_Container.Resolve<ConnectionStringsTest1>();
            Assert.AreEqual(connectionString, test1.ConnectionString);
        }

        [Test]
        public void EndpointResolveByTwoDifferentConstructorParameterNameTest()
        {
            m_Container.Register(Component.For<ConnectionStringsTest2>());

            var test1 = m_Container.Resolve<ConnectionStringsTest2>();
            Assert.AreEqual(m_ConnectionString1, test1.ConnectionString1);
            Assert.AreEqual(m_ConnectionString2, test1.ConnectionString2);
        }

        [Test]
        public void EndpointResolveByTwoDifferentOverridenParameterNameTest()
        {
            m_Container.Register(Component.For<ConnectionStringsTest2>().WithConnectionStrings(new { cs1 = "cs4", cs2 = "cs5" }));

            var test1 = m_Container.Resolve<ConnectionStringsTest2>();
            Assert.AreEqual(m_ConnectionString4, test1.ConnectionString1);
            Assert.AreEqual(m_ConnectionString5, test1.ConnectionString2);
        }

        [Test]
        public void EndpointResolveByTwoDifferentOneOverridenParameterNameTest()
        {
            m_Container.Register(Component.For<ConnectionStringsTest2>().WithConnectionStrings(new { cs2 = "cs5" }));

            var test1 = m_Container.Resolve<ConnectionStringsTest2>();
            Assert.AreEqual(m_ConnectionString1, test1.ConnectionString1);
            Assert.AreEqual(m_ConnectionString5, test1.ConnectionString2);
        }

        [Test]
        public void EndpointResolveByTwoDifferentOneOverridenAndExplicitParameterNameTest()
        {
            var connectionString = new ConnectionString("custom-connection-string");
            m_Container.Register(Component.For<ConnectionStringsTest2>().WithConnectionStrings(new { cs1 = "cs4", cs2 = connectionString }));

            var test1 = m_Container.Resolve<ConnectionStringsTest2>();
            Assert.AreEqual(m_ConnectionString4, test1.ConnectionString1);
            Assert.AreEqual(connectionString, test1.ConnectionString2);
        }

    }

    internal class ConnectionStringsTest1
    {
        public ConnectionString ConnectionString { get; private set; }

        public ConnectionStringsTest1(ConnectionString cs1)
        {
            ConnectionString = cs1;
        }
    }

    internal class ConnectionStringsTest2
    {
        public ConnectionString ConnectionString1 { get; private set; }
        public ConnectionString ConnectionString2 { get; private set; }

        public ConnectionStringsTest2(ConnectionString cs1, ConnectionString cs2)
        {
            ConnectionString1 = cs1;
            ConnectionString2 = cs2;
        }
    }
}