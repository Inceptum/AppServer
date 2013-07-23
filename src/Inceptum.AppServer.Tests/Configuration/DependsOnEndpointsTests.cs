using System;
using System.Collections.Generic;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using Inceptum.Messaging.Contract;
using NUnit.Framework;

namespace Inceptum.AppServer.Tests.Configuration
{
	[TestFixture]
	public class DependsOnEndpointsTests
	{
		private Endpoint m_Endpoint1;
		private Endpoint m_Endpoint2;
		private Endpoint m_Endpoint3;
		private Endpoint m_Endpoint4;
		private Endpoint m_Endpoint5;
		private WindsorContainer m_Container; 
		
		[SetUp]
		public void SetUp()
		{
			m_Endpoint1 = new Endpoint("transport-id-1", "destination-1");
			m_Endpoint2 = new Endpoint("transport-id-2", "destination-2");
			m_Endpoint3 = new Endpoint("transport-id-3", "destination-3");
			m_Endpoint4 = new Endpoint("transport-id-4", "destination-4");
			m_Endpoint5 = new Endpoint("transport-id-5", "destination-5");
            var endpointResolver = new EndpointResolver(new Dictionary<string, Endpoint>()
		        {
		            {"endpoint1", m_Endpoint1},
		            {"endpoint2", m_Endpoint2},
		            {"endpoint3", m_Endpoint3},
		            {"endpoint4", m_Endpoint4},
		            {"endpoint5", m_Endpoint5},
		        });

			m_Container = new WindsorContainer();
			m_Container.Kernel.Resolver.AddSubResolver(endpointResolver);
		}

		[Test]
		public void EndpointResolveByConstructorParameterNameTest()
		{
			m_Container.Register(Component.For<Test1>());
			
			var test1 = m_Container.Resolve<Test1>();
			Assert.AreEqual(m_Endpoint1.TransportId, test1.Endpoint.TransportId);
			Assert.AreEqual(m_Endpoint1.Destination, test1.Endpoint.Destination);
		}

		[Test]
		public void EndpointResolveByOverridenParameterNameTest()
		{
			m_Container.Register(Component.For<Test1>().WithEndpoints(new { endpoint1 = "endpoint2" }));
			
			var test1 = m_Container.Resolve<Test1>();
			Assert.AreEqual(m_Endpoint2.TransportId, test1.Endpoint.TransportId);
			Assert.AreEqual(m_Endpoint2.Destination, test1.Endpoint.Destination);
		}

		[Test]
		public void EndpointResolveByExplicitEndpointParameterNameTest()
		{
			var endpoint = new Endpoint( transportId: "custom-transport-id", destination:"custom-destination");
			
			m_Container.Register(Component.For<Test1>().WithEndpoints(new { endpoint1 = endpoint }));
			var test1 = m_Container.Resolve<Test1>();
			Assert.AreEqual(endpoint.TransportId, test1.Endpoint.TransportId);
			Assert.AreEqual(endpoint.Destination, test1.Endpoint.Destination);
		}

		[Test]
		public void EndpointResolveByTwoDifferentConstructorParameterNameTest()
		{
			m_Container.Register(Component.For<Test2>());

			var test1 = m_Container.Resolve<Test2>();
			Assert.AreEqual(m_Endpoint1.TransportId, test1.Endpoint1.TransportId);
			Assert.AreEqual(m_Endpoint1.Destination, test1.Endpoint1.Destination);
			Assert.AreEqual(m_Endpoint2.TransportId, test1.Endpoint2.TransportId);
			Assert.AreEqual(m_Endpoint2.Destination, test1.Endpoint2.Destination);
		}

		[Test]
		public void EndpointResolveByTwoDifferentOverridenParameterNameTest()
		{
			m_Container.Register(Component.For<Test2>().WithEndpoints(new { endpoint1 = "endpoint4", endpoint2 = "endpoint5" }));

			var test1 = m_Container.Resolve<Test2>();
			Assert.AreEqual(m_Endpoint4.TransportId, test1.Endpoint1.TransportId);
			Assert.AreEqual(m_Endpoint4.Destination, test1.Endpoint1.Destination);
			Assert.AreEqual(m_Endpoint5.TransportId, test1.Endpoint2.TransportId);
			Assert.AreEqual(m_Endpoint5.Destination, test1.Endpoint2.Destination);
		}

		[Test]
		public void EndpointResolveByTwoDifferentOneOverridenParameterNameTest()
		{
			m_Container.Register(Component.For<Test2>().WithEndpoints(new { endpoint2 = "endpoint5" }));

			var test1 = m_Container.Resolve<Test2>();
			Assert.AreEqual(m_Endpoint1.TransportId, test1.Endpoint1.TransportId);
			Assert.AreEqual(m_Endpoint1.Destination, test1.Endpoint1.Destination);
			Assert.AreEqual(m_Endpoint5.TransportId, test1.Endpoint2.TransportId);
			Assert.AreEqual(m_Endpoint5.Destination, test1.Endpoint2.Destination);
		}

		[Test]
		public void EndpointResolveByTwoDifferentOneOverridenAndExplicitParameterNameTest()
		{
			var endpoint = new Endpoint(transportId: "custom-transport-id", destination: "custom-destination");

			m_Container.Register(Component.For<Test2>().WithEndpoints(new { endpoint1 = "endpoint4", endpoint2 = endpoint }));

			var test1 = m_Container.Resolve<Test2>();
			Assert.AreEqual(m_Endpoint4.TransportId, test1.Endpoint1.TransportId);
			Assert.AreEqual(m_Endpoint4.Destination, test1.Endpoint1.Destination);
			Assert.AreEqual(endpoint.TransportId, test1.Endpoint2.TransportId);
			Assert.AreEqual(endpoint.Destination, test1.Endpoint2.Destination);
		}
	}

	internal class Test1
	{
		public Endpoint Endpoint { get; private set; }

		public Test1(Endpoint endpoint1)
		{
			Endpoint = endpoint1;
		}
	}

	internal class Test2
	{
		public Endpoint Endpoint1 { get; private set; }
		public Endpoint Endpoint2 { get; private set; }

		public Test2(Endpoint endpoint1, Endpoint endpoint2)
		{
			Endpoint1 = endpoint1;
			Endpoint2 = endpoint2;
		}
	}
}