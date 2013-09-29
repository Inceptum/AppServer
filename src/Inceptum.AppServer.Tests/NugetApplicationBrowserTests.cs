using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery.Nuget;
using NUnit.Framework;

namespace Inceptum.AppServer.Tests
{
    [TestFixture]
    public class NugetApplicationBrowserTests
    {
        [Test]
        public void Test()
        {
            var assembly = GetType().Assembly;
            var codebase = assembly.CodeBase.Replace("file:///", "");
            var baseDir = Path.GetDirectoryName(codebase);
            Directory.SetCurrentDirectory(baseDir);

            var browser = new NugetApplicationBrowser(NullLogger.Instance,"..\\..\\..\\..\\TestData\\NugetRepo", "https://nuget.org/api/v2/");
            var hostedAppInfos = browser.GetAvailableApps().ToArray();
            Console.WriteLine("");
        }
    }
}