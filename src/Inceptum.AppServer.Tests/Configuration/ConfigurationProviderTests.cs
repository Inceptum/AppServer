using Castle.Core.Logging;
using Inceptum.AppServer;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Providers;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.Configuration.Tests.Client
{
    [TestFixture]
    public class ConfigurationProviderTests
    {
        [Test]
        public void AppServerConfigIsAlwaysLocalAndShouldBeNeverCachedTest()
        {
            var extProvider = MockRepository.GenerateMock<IManageableConfigurationProvider>();
            var localProvider = MockRepository.GenerateMock<IManageableConfigurationProvider>();
            localProvider.Expect(p => p.GetBundle(null, null, null)).IgnoreArguments().Return("a=b");
            var fsProvider = MockRepository.GenerateMock<FileSystemConfigurationProvider>(".");
            var provider = new AppServerExternalConfigurationProvider(NullLogger.Instance, localProvider, new CachingRemoteConfigurationProvider(fsProvider, extProvider, NullLogger.Instance));
            var bundle = provider.GetBundle("appserver", "test", "param1", "param2");
            localProvider.AssertWasCalled((p => p.GetBundle("appserver", "test", "param1", "param2")));
            extProvider.AssertWasNotCalled((p => p.GetBundle("appserver", "test", "param1", "param2")));
            fsProvider.AssertWasNotCalled((p => p.GetBundle("appserver", "test", "param1", "param2")));
            fsProvider.AssertWasNotCalled((p => p.StoreBundle("appserver", "test", new[] { "param1", "param2" }, "a=b")));
            Assert.That(bundle, Is.EqualTo("a=b"), "wrong content was received");
        }
        [Test]
        public void LoadFromExternalProviderTest()
        {
            var extProvider = MockRepository.GenerateMock<IManageableConfigurationProvider>();
            var localProvider = MockRepository.GenerateMock<IManageableConfigurationProvider>();
            extProvider.Expect(p => p.GetBundle(null, null, null)).IgnoreArguments().Return("a=b");
            var fsProvider = MockRepository.GenerateMock<FileSystemConfigurationProvider>(".");
            var provider = new AppServerExternalConfigurationProvider(NullLogger.Instance,localProvider, new CachingRemoteConfigurationProvider(fsProvider, extProvider, NullLogger.Instance));
            var bundle = provider.GetBundle("testConfiguration", "test", "param1", "param2");
            extProvider.AssertWasCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasNotCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasCalled((p => p.StoreBundle("testConfiguration", "test", new[] { "param1", "param2" }, "a=b")));
            Assert.That(bundle, Is.EqualTo("a=b"), "wrong content was received");
        }

        [Test]
        public void LoadFromCacheTest()
        {
            var localProvider = MockRepository.GenerateMock<IManageableConfigurationProvider>();
            var extProvider = MockRepository.GenerateMock<IManageableConfigurationProvider>();
            extProvider.Expect(p => p.GetBundle(null, null, null)).IgnoreArguments().Return(null);
            var fsProvider = MockRepository.GenerateMock<FileSystemConfigurationProvider>(".");
            fsProvider.Expect(p => p.GetBundle(null, null, null)).IgnoreArguments().Return("a=b");
            var provider = new AppServerExternalConfigurationProvider(NullLogger.Instance,localProvider, new CachingRemoteConfigurationProvider(fsProvider, extProvider, NullLogger.Instance));

            var bundle = provider.GetBundle("testConfiguration","test", "param1", "param2");

            extProvider.AssertWasCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasNotCalled((p => p.StoreBundle("testConfiguration", "test", new[] { "param1", "param2" }, "a=b")));
            Assert.That(bundle, Is.EqualTo("a=b"), "wrong content was received");
        }
        
       
    }
}