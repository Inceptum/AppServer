using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery.NuGet;
using NuGet;
using NUnit.Framework;

namespace Inceptum.AppServer.Tests.AppDiscovery.NuGet
{
    [Ignore]
    [TestFixture]
    public class ApplicationProjectManagerTests
    {
        public void Upgrade(ProjectManagerWrapper projectManager, string packageId)
        {
            var installedPackage = getInstalledPackage(projectManager, packageId);
            var update = projectManager.GetUpdate(installedPackage).First();
            projectManager.UpdatePackage(update);
        }

        [Test]
        public void InstallTest()
        {
            var tempPath = @"e:\AppsTest\raven";
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            Directory.CreateDirectory(tempPath);
            Console.WriteLine(tempPath);

            var applicationRepository = "http://nuget.it.unistreambank.ru/nuget/PROD.Apps";
            var dependenciesRepositories = new[] {"http://nuget.it.unistreambank.ru/nuget/PROD.Libs"};

            var appRepository = PackageRepositoryFactory.Default.CreateRepository(applicationRepository);
            var dependencyRepositories = dependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();

            var dependenciesRepository = new AggregateRepository(new[] {appRepository}.Concat(dependencyRepositories));

            var sharedRepositoryDir = Path.Combine(tempPath, "packages");

            var consoleLogger = new ConsoleLogger();

            var applicationProjectManager = new ProjectManagerWrapper("Unistream.Processing.Operations", 
                sharedRepositoryDir, tempPath, consoleLogger, dependenciesRepository, DependencyVersion.Highest, true);

            var sw = Stopwatch.StartNew();
            applicationProjectManager.InstallPackage("Unistream.Processing.Operations", new SemanticVersion("1.0.9.46"));
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [Test]
        public void UpgradeTest()
        {
            var tempPath = @"e:\AppsTest\operations";
            if (!Directory.Exists(tempPath))
            {
                Directory.CreateDirectory(tempPath);
            }

            Console.WriteLine(tempPath);

            var applicationProjectManager = new ProjectManagerWrapper("Unistream.Processing.Operations", Path.Combine(tempPath, "packages"), tempPath, new ConsoleLogger(),
                PackageRepositoryFactory.Default.CreateRepository(@"d:\AppsTest\Repo30"), DependencyVersion.Highest, true);

            var installedPackage = getInstalledPackage(applicationProjectManager, "Unistream.Processing.Operations");
            var update = applicationProjectManager.GetUpdate(installedPackage);

            Console.WriteLine("UPGRADING FROM {0} TO {1}", installedPackage.Version, update.First().Version);
            Upgrade(applicationProjectManager, "Unistream.Processing.Operations");
        }

        private static IPackage getInstalledPackage(ProjectManagerWrapper projectManager, string packageId)
        {
            var package = projectManager.GetInstalledPackages(packageId).FirstOrDefault(p => p.Id == packageId);
            if (package == null)
            {
                throw new InvalidOperationException(string.Format("The package for package ID '{0}' is not installed.", packageId));
            }
            return package;
        }
    }
}