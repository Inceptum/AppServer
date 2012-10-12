using System;
using Castle.Core;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Inceptum.AppServer.Configuration;
using NUnit.Framework;
using Rhino.Mocks;
using System.Reactive.Disposables;

namespace Inceptum.Configuration.Tests.Client
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
            ComponentModel model=null;
            windsorContainer.Kernel.ComponentModelCreated += m => model = m;
            windsorContainer.Register(Component.For<IConfigurationFacility>().Instance(configurationFacility));
            windsorContainer.Register(Component.For<IDisposable>().ImplementedBy<CompositeDisposable>().FromConfiguration<IDisposable,CompositeDisposable>("bundle", "json.path", "param1", "param2"));
            var resolved = windsorContainer.Resolve<IDisposable>();
            Assert.That(resolved,Is.EqualTo(fakeBundleDeserializationResult),"Bundle deserialized value was not registered");
            configurationFacility.VerifyAllExpectations();
        }
    }
}