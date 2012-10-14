using Castle.Core.Logging;
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
        public void LoadFromExternalProviderTest()
        {
            var extProvider = MockRepository.GenerateMock<IConfigurationProvider>();
            extProvider.Expect(p => p.GetBundle(null, null, null)).IgnoreArguments().Return("a=b");
            var fsProvider = MockRepository.GenerateMock<FileSystemConfigurationProvider>(".");
            var provider = new CachingRemoteConfigurationProvider(fsProvider, extProvider, NullLogger.Instance);
            var bundle = provider.GetBundle("testConfiguration", "test", "param1", "param2");
            extProvider.AssertWasCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasNotCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasCalled((p => p.StoreBundle("testConfiguration", "test", new[] { "param1", "param2" }, "a=b")));
            Assert.That(bundle, Is.EqualTo("a=b"), "wrong content was received");
        }

        [Test]
        public void LoadFromCacheTest()
        {
            var extProvider = MockRepository.GenerateMock<IConfigurationProvider>();
            extProvider.Expect(p => p.GetBundle(null, null, null)).IgnoreArguments().Return(null);
            var fsProvider = MockRepository.GenerateMock<FileSystemConfigurationProvider>(".");
            fsProvider.Expect(p => p.GetBundle(null, null, null)).IgnoreArguments().Return("a=b");
            var provider = new CachingRemoteConfigurationProvider(fsProvider, extProvider, NullLogger.Instance);

            var bundle = provider.GetBundle("testConfiguration","test", "param1", "param2");

            extProvider.AssertWasCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasCalled((p => p.GetBundle("testConfiguration", "test", "param1", "param2")));
            fsProvider.AssertWasNotCalled((p => p.StoreBundle("testConfiguration", "test", new[] { "param1", "param2" }, "a=b")));
            Assert.That(bundle, Is.EqualTo("a=b"), "wrong content was received");
        }
        
       
    }
}