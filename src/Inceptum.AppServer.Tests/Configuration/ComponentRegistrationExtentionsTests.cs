using System;
using System.Reactive.Disposables;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.AppServer.Tests.Configuration
{
    [TestFixture]
    public class ComponentRegistrationExtentionsTests
    {
        [Test]
        public void DependsOnBundleTest()
        {
            var windsorContainer = new WindsorContainer();
            ComponentModel model=null;
            windsorContainer.Kernel.ComponentModelCreated += m => model = m;
            windsorContainer.Register(Component.For<IDisposable>().DependsOnBundle("bundle", "json.path", "param1","param2"));
            Assert.That(model.ExtendedProperties["dependsOnBundle"] as string, Is.EqualTo("bundle"), "dependsOnBundle extended property was not set");
            Assert.That(model.ExtendedProperties["jsonPath"] as string, Is.EqualTo("json.path"), "jsonPath extended property was not set");
            Assert.That(model.ExtendedProperties["bundleParameters"] as string[], Is.EquivalentTo(new[] { "param1", "param2" }), "bundleParameters extended property was not set");
        }       
        
        
        [Test]
        public void FromConfigurationTest()
        {
            var configurationFacility = MockRepository.GenerateMock<IConfigurationFacility>();
            var fakeBundleDeserializationResult = new CompositeDisposable();
            configurationFacility.Expect(
                c => c.DeserializeFromBundle<CompositeDisposable>(null,"bundle", "json.path", new[] { "param1", "param2" })).Return(fakeBundleDeserializationResult).Repeat.Once();

            var windsorContainer = new WindsorContainer();
            windsorContainer.Register(Component.For<IConfigurationFacility>().Instance(configurationFacility));
            windsorContainer.Register(Component.For<IDisposable>().ImplementedBy<CompositeDisposable>().FromConfiguration<IDisposable,CompositeDisposable>("bundle", "json.path", "param1", "param2"));
            var resolved = windsorContainer.Resolve<IDisposable>();
            Assert.That(resolved,Is.EqualTo(fakeBundleDeserializationResult),"Bundle deserialized value was not registered");
            configurationFacility.VerifyAllExpectations();
        }
   

        [Test]
        public void FromConfigurationLiveTest()
        {
            var configurationFacility = MockRepository.GenerateMock<IConfigurationFacility>();
            var fakeBundleDeserializationResult1 = new Config { Value = "value1" };
            var fakeBundleDeserializationResult2 = new Config { Value = "value2" };
            configurationFacility.Expect(
                c => c.DeserializeFromBundle<Config>(null, "bundle", "json.path", new[] { "param1", "param2" })).Return(fakeBundleDeserializationResult1).Repeat.Once();
            configurationFacility.Expect(
                c => c.DeserializeFromBundle<Config>(null, "bundle", "json.path", new[] { "param1", "param2" })).Return(fakeBundleDeserializationResult2).Repeat.Once();

            var windsorContainer = new WindsorContainer();
            windsorContainer.Register(Component.For<IConfigurationFacility>().Instance(configurationFacility));
            windsorContainer.Register(Component.For<Config>().FromLiveConfiguration<Config>("bundle", "json.path", "param1", "param2"));
            var resolved = windsorContainer.Resolve<Config>();

            Assert.That(resolved.Value, Is.EqualTo("value1"), "deserialized value is wrong");
            Assert.That(resolved.Value, Is.EqualTo("value2"), "second deserialization has not reloaded values");
            configurationFacility.VerifyAllExpectations();
        }

        [Test]
        [ExpectedException(typeof(InvalidOperationException), ExpectedMessage = "All property getters of type Inceptum.AppServer.Tests.Configuration.ConfigWithNonVirtualMembers should be virtual")]
        public void FromConfigurationLiveNonVirtualMembersFailureTest()
        {
            var windsorContainer = new WindsorContainer();
            windsorContainer.Register(Component.For<ConfigWithNonVirtualMembers>().FromLiveConfiguration<ConfigWithNonVirtualMembers>("bundle", "json.path", "param1", "param2"));
        }
    }
}