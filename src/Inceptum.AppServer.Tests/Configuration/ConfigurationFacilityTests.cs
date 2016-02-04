using System;
using System.Configuration;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.AppServer.Tests.Configuration
{
    [TestFixture]
    public class ConfigurationFacilityTests
    {
        [Test]
        public void FacilityConfigurationTest()
        {
            Assert.Catch<ConfigurationErrorsException>(() => new WindsorContainer().AddFacility<ConfigurationFacility>(), "ConfigurationFacility is not set up correctly. You have to provide Configuration and ServiceUrl in onCreate");
            Assert.Catch<ConfigurationErrorsException>(() => new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Configuration("ibank")), "ConfigurationFacility is not set up correctly. You have to provide Configuration and ServiceUrl in onCreate");
            Assert.Catch<ConfigurationErrorsException>(() => new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Remote("http://localhost:8080/")), "ConfigurationFacility is not set up correctly. You have to provide Configuration and ServiceUrl in onCreate");
            Assert.Catch<ArgumentException>(() => new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Configuration("#i b #%^#ank")));
            Assert.Catch<ArgumentException>(() => new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Remote("#i b #%^#ank")));
            Assert.Catch<ConfigurationErrorsException>(() => new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Configuration("ibank")), "IConfigurationProvider not found. Register it before registering ConfigurationFacility");
         }

        [TestCase("prop","{\"prop\":{\"longDependency\":999999999999,\"intDependency\":10, \"strDependency\":\"str\"}}",TestName = "with json path ")]
        [TestCase("", "{\"longDependency\":999999999999,\"intDependency\":10, \"strDependency\":\"str\"}", TestName = "without json path ")]
        public void DependencyOnConfigurationTest(string jsonPath, string bundleContent)
        {
            var configurationProvider = MockRepository.GenerateMock<IConfigurationProvider>();
            configurationProvider.Expect(p => p.GetBundle("testConfiguration", "bundle", "param1", "param2")).Return(
                bundleContent);
            var windsorContainer = new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Remote("http://localhost:8080/").Configuration("testConfiguration").Provider = configurationProvider);
            var resolved = windsorContainer.Register(Component.For<TestComponent>().DependsOnBundle("bundle", jsonPath, "param1","param2")).Resolve<TestComponent>();
            Assert.That(resolved.LongDependency, Is.EqualTo(999999999999));
            Assert.That(resolved.IntDependency, Is.EqualTo(10));
            Assert.That(resolved.StrDependency, Is.EqualTo("str"));
        }

        [TestCase("prop", "{\"prop\":{\"longDependency\":999999999999,\"intDependency\":10, \"strDependency\":\"str\"}}", TestName = "with json path")]
        [TestCase("", "{\"longDependency\":999999999999,\"intDependency\":10, \"strDependency\":\"str\"}", TestName = "without json path")]
        public void DependencyOnConfigurationWithServiceTest(string jsonPath, string bundleContent)
        {
            var configurationProvider = MockRepository.GenerateMock<IConfigurationProvider>();
            configurationProvider.Expect(p => p.GetBundle("testConfiguration", "bundle", "param1", "param2")).Return(
                bundleContent);
            var windsorContainer = new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Remote("http://localhost:8080/").Configuration("testConfiguration").Provider = configurationProvider);
            var resolved = windsorContainer.Register(Component.For<ITestComponent>().ImplementedBy<TestComponent>().DependsOnBundle("bundle", jsonPath, "param1", "param2")).Resolve<ITestComponent>();
            Assert.That(resolved.LongDependency, Is.EqualTo(999999999999));
            Assert.That(resolved.IntDependency, Is.EqualTo(10));
            Assert.That(resolved.StrDependency, Is.EqualTo("str"));
        }
        
        [Test]
        public void DependencyOnConfigurationParameteresTest()
        {
            var configurationProvider = MockRepository.GenerateMock<IConfigurationProvider>();
            configurationProvider.Expect(p => p.GetBundle("testConfiguration", "bundle", "dit", "msa-ibdev1")).Return("{\"prop\":{\"longDependency\":999999999999,\"intDependency\":10, \"strDependency\":\"str\"}}");
            var windsorContainer = new WindsorContainer().AddFacility<ConfigurationFacility>(f => f.Remote("http://localhost:8080/").Configuration("testConfiguration").Params(new { environment = "dit", box = "msa-ibdev1" }).Provider = configurationProvider);
            var resolved = windsorContainer.Register(Component.For<TestComponent>().DependsOnBundle("bundle", "prop", "{environment}", "{box}")).Resolve<TestComponent>();

            Assert.That(resolved.LongDependency, Is.EqualTo(999999999999));
            Assert.That(resolved.IntDependency, Is.EqualTo(10));
            Assert.That(resolved.StrDependency, Is.EqualTo("str"));
        }

        [Test]
        public void DependencyOnConfigurationParameteresTest2()
        {
            var configurationProvider = MockRepository.GenerateMock<IConfigurationProvider>();

            configurationProvider.Expect(p => 
                    p.GetBundle("testConfiguration", "bundle", "dit", "msa-ibdev1"))
                     .Return("{'prop':{'timeout':'00:00:30', 'connectionString': 'http://never.land/api/v31415'}}");

            TestComponent2 resolved;
            using (var container = new WindsorContainer())
            {
                var windsorContainer = container.AddFacility<ConfigurationFacility>(f =>
                {
                    var configurationFacility = f.Remote("http://localhost:8080/").Configuration("testConfiguration").Params(new {environment = "dit", box = "msa-ibdev1"});
                    configurationFacility.Provider = configurationProvider;
                });

                resolved = windsorContainer.Register(Component.For<TestComponent2>().DependsOnBundle("bundle", "prop", "{environment}", "{box}")).Resolve<TestComponent2>();
            }

            Assert.AreEqual(resolved.ConnectionString, "http://never.land/api/v31415");
            Assert.AreEqual(resolved.Timeout, TimeSpan.FromSeconds(30));
        }

        public interface ITestComponent
        {
            long LongDependency { get; }
            string StrDependency { get; }
            int IntDependency { get; }
        }

        public class TestComponent : ITestComponent
        {
            private readonly long m_LongDependency;
            private readonly string m_StrDependency;
            private int m_IntDependency;

            public long LongDependency
            {
                get { return m_LongDependency; }
            }

            public string StrDependency
            {
                get { return m_StrDependency; }
            }

            public int IntDependency
            {
                get { return m_IntDependency; }
                set { m_IntDependency = value; }
            }

            public TestComponent(string strDependency, long longDependency, int intDependency)
            {
                m_IntDependency = intDependency;
                m_StrDependency = strDependency;
                m_LongDependency = longDependency;
            }
        }

        public class TestComponent2
        {
            public string ConnectionString { get; set; }

            public TimeSpan Timeout { get; set; }

            public TestComponent2(string connectionString, TimeSpan timeout)
            {
                ConnectionString = connectionString;
                Timeout = timeout;
            }
        }
    }
}