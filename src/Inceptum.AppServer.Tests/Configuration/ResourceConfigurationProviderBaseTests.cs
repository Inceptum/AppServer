using System;
using Inceptum.AppServer.Configuration.Providers;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.AppServer.Tests.Configuration
{
    [TestFixture]
    public class ResourceConfigurationProviderBaseTests
    {
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void GetBundleNameIsNullFailureTest()
        {
            var provider = MockRepository.GeneratePartialMock<ResourceConfigurationProviderBase>();
            provider.Expect(p => p.GetResourceContent("")).IgnoreArguments();
            provider.Expect(p => p.GetResourceName("testConfiguration", "")).IgnoreArguments();

            provider.GetBundle("testConfiguration", null);

        }       
        
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void GetBundleNameIsEmptyFailureTest()
        {
            var provider = MockRepository.GeneratePartialMock<ResourceConfigurationProviderBase>();
            provider.Expect(p => p.GetResourceContent("")).IgnoreArguments();
            provider.Expect(p => p.GetResourceName("testConfiguration", "")).IgnoreArguments();

            provider.GetBundle("testConfiguration", "");

        }
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void GetBundleEmptyParamFailureTest()
        {
            var provider = MockRepository.GeneratePartialMock<ResourceConfigurationProviderBase>();
            provider.Expect(p => p.GetResourceContent("")).IgnoreArguments();
            provider.Expect(p => p.GetResourceName("testConfiguration", "")).IgnoreArguments();

            provider.GetBundle("testConfiguration", "bundle", "");

        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void GetBundleNullParamFailureTest()
        {
            var provider = MockRepository.GeneratePartialMock<ResourceConfigurationProviderBase>();
            provider.Expect(p => p.GetResourceContent("")).IgnoreArguments();
            provider.Expect(p => p.GetResourceName("testConfiguration", "")).IgnoreArguments();

            provider.GetBundle("testConfiguration", "bundle", "");

        }


        [Test]
        public void GetBundleParamValuesAreTrimmedTest()
        {
            var provider = MockRepository.GeneratePartialMock<ResourceConfigurationProviderBase>();
            provider.Expect(p => p.GetResourceContent("")).IgnoreArguments().Return("aa");
            provider.Expect(p => p.GetResourceName("testConfiguration", "")).IgnoreArguments().Return("aa");

            provider.GetBundle("testConfiguration", " bundle ", " test ");

            provider.AssertWasCalled(p => p.GetResourceName("testConfiguration", "bundle", "test"));

        }
    }
}