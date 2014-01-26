using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery.Nuget;
using Inceptum.AppServer.AppDiscovery.Nuget.old;
using NUnit.Framework;
using NuGet;
using NullLogger = Castle.Core.Logging.NullLogger;

namespace Inceptum.AppServer.Tests
{
    [TestFixture]
    public class NugetApplicationBrowserTests
    {
        [Test]
        [Ignore]
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
        [Test]
        [Ignore]
        public void Test3()
        {
            IPackageRepository appsRepo = PackageRepositoryFactory.Default.CreateRepository("http://nuget.it.unistreambank.ru/nuget/DEV.Apps");
            var apps = appsRepo.GetPackages().Where(p => p.Tags != null && p.Tags.Contains("Inceptum.AppServer.Application")).OrderBy(p=>p.Id);
            
            foreach (var p in apps)
            {
                Console.WriteLine(p.Id);
            }
            
        }

        [Test]
        [Ignore]
        public void Test2()
        {
            var assembly = GetType().Assembly;
            var codebase = assembly.CodeBase.Replace("file:///", "");
            var baseDir = Path.GetDirectoryName(codebase);
            Directory.SetCurrentDirectory(baseDir);

            var browser = new NugetApplicationBrowser(new ConsoleLogger(), "http://nuget.it.unistreambank.ru/nuget/DEV.Apps", "http://nuget.it.unistreambank.ru/nuget/DEV.Libs");
            var hostedAppInfos = browser.GetAvailableApps().ToArray();
            foreach (var hostedAppInfo in hostedAppInfos)
            {
                Console.WriteLine(hostedAppInfo.Name);
            }
            Console.WriteLine("");
        }
    }
}