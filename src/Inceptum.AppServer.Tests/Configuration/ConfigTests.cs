using System;
using System.Collections.Generic;
using System.Linq;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Configuration.Model;
using Inceptum.AppServer.Configuration.Providers;
using NUnit.Framework;
using Rhino.Mocks;

namespace Inceptum.AppServer.Tests.Configuration
{
    [TestFixture]
    public class ConfigTests
    {
        [Test]
        public void CtorTest()
        {
            var contentProcessor = MockRepository.GenerateMock<IContentProcessor>();
            var persister = MockRepository.GenerateMock<IConfigurationPersister>();
            var bundles=new Dictionary<string, string>
                {
                    {"A.B.C", "c"},
                    {"A.B", "b"},
                    {"A", "a"}
                };

            var config = new Config(persister, contentProcessor, "test", bundles);
            var list = new Dictionary<string,Bundle>();
            config.Visit(b=>list.Add(b.Name,b));
            Assert.That(list.Count(), Is.EqualTo(3), "wrong number of bundles were created");
            Assert.That(list.Keys.ToArray(), Is.EquivalentTo(new[] { "a", "a.b", "a.b.c" }), "wrong bundle names were created");
            Assert.That(list.Values.Select(b => b.PureContent).ToArray(), Is.EquivalentTo(new[] { "a", "b", "c" }), "wrong bundles content");
            Assert.That(list["a.b.c"].Parent, Is.SameAs(list["a.b"]), "wrong bundles structure");
            Assert.That(list["a.b"].Parent, Is.SameAs(list["a"]), "wrong bundles structure");
            Assert.That(list["a"].Parent, Is.Null, "wrong bundles structure");
            Assert.That(new[] { config["a"], config["a.b"], config["a.b.c"] }, Is.EquivalentTo(new[] { "a", "ab", "abc" }), "wrong bundles content");

        }
        

         
    }
}