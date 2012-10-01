using System;
using System.Linq;
using Inceptum.AppServer.Model;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.Tests
{
    [TestFixture]
    public class Investigation
    {
        [Test]
        public void ListCompareTest()
        {
            var names = Enumerable.Range(1, 6).Select(i => "n" + i).Concat(new[]{"n1"}).ToArray();

            var configs = new[]
                              {
                                  new InstanceConfig {Name="n2",ApplicationId = "a2", Version = new Version(1, 0, 0, 0)},
                                  new InstanceConfig {Name="n3",ApplicationId = "a3", Version = new Version(1, 0, 0, 0)},
                                  new InstanceConfig {Name="n4",ApplicationId = "a4", Version = new Version(1, 0, 0, 0)},
                                  new InstanceConfig {Name="n5",ApplicationId = "a5", Version = new Version(1, 0, 0, 0)},
                                  new InstanceConfig {Name="n6",ApplicationId = "b4", Version = new Version(1, 3, 0, 0)},
                              };
            var m_InstancesConfiguration = new[]
                              {
                                  new InstanceConfig {Name="n1",ApplicationId = "a1", Version = new Version(1, 0, 0, 0)},
                                  new InstanceConfig {Name="n2",ApplicationId = "b2", Version = new Version(1, 0, 0, 0)},
                                  new InstanceConfig {Name="n3",ApplicationId = "a3", Version = new Version(1, 2, 0, 0)},
                                  new InstanceConfig {Name="n4",ApplicationId = "b4", Version = new Version(1, 3, 0, 0)},
                                  new InstanceConfig {Name="n6",ApplicationId = "b4", Version = new Version(1, 3, 0, 0)},
                              };

            var updated = from newConfig in configs
                          join oldConfig in m_InstancesConfiguration on newConfig.Name equals oldConfig.Name into changed
                          from oldConfig in changed
                          where newConfig.ApplicationId != oldConfig.ApplicationId || newConfig.Version != oldConfig.Version
                          from name in names
                          where name==newConfig.Name
                          select name;

            var added = from newConfig in configs
                        join oldConfig in m_InstancesConfiguration on newConfig.Name equals oldConfig.Name into changed
                        from oldConfig in changed.DefaultIfEmpty()
                        where oldConfig == null
                          from name in names
                          where name==newConfig.Name
                          select name;

            var deleted = from oldConfig in m_InstancesConfiguration
                          join newConfig in configs on oldConfig.Name equals newConfig.Name into changed
                          from newConfig in changed.DefaultIfEmpty()
                          where newConfig == null
                          from name in names
                          where name==oldConfig.Name
                          select name;

            Console.WriteLine(string.Join(",", updated));
            Console.WriteLine(string.Join(",", added));
            Console.WriteLine(string.Join(",", deleted));

        }

        [Test]
         public void Test()
        {
            Console.WriteLine(
                JObject.Parse(@"{
""aaa"":""111


2
"",
   ""CryptoProvider"":{
      ""ca"":""



-----BEGIN CERTIFICATE-----
MIIBcDCCAROgAwIBAgIIAbYBAQEHAQEwDgYKKwYBBAGtWQEDAgUAMBMxETAPBgNV
BAMUCEFkbWluX0NBMB4XDTEwMDgxMjE3MzQwMFoXDTE1MDgxMTE3MzQwMFowEzER
MA8GA1UEAxQIQWRtaW5fQ0EwXjAWBgorBgEEAa1ZAQYCBggqhkjOPQMBBwNEAARB
BL2tXRwk9O5KR/KVzxk4Mshbu5Nfl0fhE21P3gH8weSano70MXCGl7HMYvrc4T3X
n5mDXLwBJTdfgYh+O3I/AUijSDBGMBIGA1UdEwEB/wQIMAYBAf8CAQAwIAYDVR0O
AQH/BBYEFIaLPiDvnQCaBv2L+pHtyJ8NSE1QMA4GA1UdDwEB/wQEAwIBBjAOBgor
BgEEAa1ZAQMCBQADRwAwRAIgUdNtSluRdsQED/0c7anqg5QH0SJzeD9Tmy4keAh0
WWkCICCW4uD/IFcWFvfkpqYvgj4/FQoaqTi5O6fu8NE6HVXl





-----END CERTIFICATE-----""
   }
}
"));
        }
    }
}