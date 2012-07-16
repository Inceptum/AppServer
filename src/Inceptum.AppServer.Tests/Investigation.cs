using System;
using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inceptum.AppServer.Tests
{
    [TestFixture]
    public class Investigation
    {
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