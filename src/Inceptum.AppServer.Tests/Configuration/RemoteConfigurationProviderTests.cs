using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Inceptum.AppServer.Configuration.Providers;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.Configuration.Tests.Client
{
    [TestFixture]
    public class RemoteConfigurationProviderTests
    {
        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void UrlIsNullCtorFailureTest()
        {
            new RemoteConfigurationProvider(null);
        }
        [Test]
        [ExpectedException(typeof(ArgumentException), ExpectedMessage = "Wrong URL format\r\nParameter name: configurationServiceUrl")]
        public void UrlFormatCtorFailureTest()
        {
            new RemoteConfigurationProvider("abc.com/dd/");
        }

        [Test]
        public void TrailingSlashAppendCtorTest()
        {
            var provider = new RemoteConfigurationProvider("http://abc.com/dd");
            Assert.AreEqual("http://abc.com/dd/",provider.ConfigurationServiceUrl.AbsoluteUri,"Trailing slash was not added to the url");
        }

        [Test]
        public void GetResourceNameNoExtraParamsTest()
        {
            var provider =new RemoteConfigurationProvider("http://abc.com/dd/");
            var resourceName = provider.GetResourceName("testConfiguration", "ibank");
            Assert.That(resourceName, Is.EqualTo("testConfiguration/ibank/"), "Wrong resource name  was generated");
        }

        [Test]
        public void GetResourceNameExtraParamsTest()
        {
            var provider = new RemoteConfigurationProvider("http://abc.com/dd/");
            var resourceName = provider.GetResourceName("testConfiguration", "ibank", "test1", "test2");
            Assert.That(resourceName, Is.EqualTo("testConfiguration/ibank/test1/test2/"),"Wrong resource name  was generated");
            resourceName = provider.GetResourceName("testConfiguration", "ibank", "test1");
            Assert.That(resourceName, Is.EqualTo("testConfiguration/ibank/test1/"),"Wrong resource name  was generated");

        }



    }
}
