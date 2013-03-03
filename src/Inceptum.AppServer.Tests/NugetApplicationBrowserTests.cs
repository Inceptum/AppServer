using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            var browser = new NugetApplicationBrowser();
            var hostedAppInfos = browser.GetAvailabelApps().ToArray();
            Console.WriteLine("");
        }
    }
}