using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery.NuGet;
using NuGet;
using NUnit.Framework;

using Path=System.IO.Path;
namespace Inceptum.AppServer.Tests.NuGetInstaller
{
    [TestFixture]
    [Ignore]
    public class ApplicationProjectManagerTests
    {
        [Test]

        public void InstallTest()
        {

            
         /*   string tempPath =  Path.GetTempFileName();
            File.Delete(tempPath);
            Directory.CreateDirectory(tempPath);*/

            var tempPath = @"d:\AppsTest\raven";
            if(Directory.Exists(tempPath))
                Directory.Delete(tempPath,true);
                
            Directory.CreateDirectory(tempPath);
            Console.WriteLine(tempPath);

            var applicationRepository = "http://nuget.it.unistreambank.ru/nuget/PROD.Apps";
            var dependenciesRepositories = new string[] { "http://nuget.it.unistreambank.ru/nuget/PROD.Libs" }; ;

            var appRepository = PackageRepositoryFactory.Default.CreateRepository(applicationRepository);
            IPackageRepository[] dependencyRepositories = dependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();

            var dependenciesRepository = new AggregateRepository(new[] { appRepository}.Concat(dependencyRepositories)
                );

            var applicationProjectManager = new ProjectManagerWrapper("Unistream.Processing.Operations", Path.Combine(tempPath, "packages"), tempPath, new ConsoleLogger(), dependenciesRepository);
            Stopwatch sw=Stopwatch.StartNew();
            //var applicationProjectManager = new ProjectManagerWrapper("RavenDB.Database", Path.Combine(tempPath, "packages"), tempPath, new ConsoleLogger(), PackageRepositoryFactory.Default.CreateRepository(@"d:\AppsTest\Repo23"));
            applicationProjectManager.InstallPackage("Unistream.Processing.Operations", new SemanticVersion("1.0.0.569"));
/*
            var applicationRepository = "http://nuget.it.unistreambank.ru/nuget/DEV.Apps";
            var dependenciesRepositories = new string[]{"http://nuget.it.unistreambank.ru/nuget/DEV.Libs", "http://nuget.it.unistreambank.ru/nuget/DEV.ThirdParty" }; ;

            var appRepository = PackageRepositoryFactory.Default.CreateRepository(applicationRepository);
            IPackageRepository[] dependencyRepositories = dependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();

            var dependenciesRepository = new AggregateRepository(new[] { appRepository}.Concat(dependencyRepositories)
                );

            var applicationProjectManager = new ProjectManagerWrapper("Unistream.Integration.PaymentCenter.Hooks.ClientsManagement", Path.Combine(tempPath, "packages"), tempPath, new ConsoleLogger(), dependenciesRepository);
            Stopwatch sw=Stopwatch.StartNew();
            //var applicationProjectManager = new ProjectManagerWrapper("RavenDB.Database", Path.Combine(tempPath, "packages"), tempPath, new ConsoleLogger(), PackageRepositoryFactory.Default.CreateRepository(@"d:\AppsTest\Repo23"));
            applicationProjectManager.InstallPackage("Unistream.Integration.PaymentCenter.Hooks.ClientsManagement", new SemanticVersion("1.0.2.17"));
*/
            Console.WriteLine(sw.ElapsedMilliseconds);
        }

        [Test]
        public void UpgradeTest()
        {
         /*   string tempPath =  Path.GetTempFileName();
            File.Delete(tempPath);
            Directory.CreateDirectory(tempPath);*/

            var tempPath = @"d:\AppsTest\operations";
            if(!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);
            Console.WriteLine(tempPath);
            //var applicationProjectManager = new ApplicationProjectManager(tempPath, "http://nuget.it.unistreambank.ru/nuget/DEV.Apps", "http://nuget.it.unistreambank.ru/nuget/DEV.Libs", "http://nuget.it.unistreambank.ru/nuget/DEV.ThirdParty");
            var applicationProjectManager = new ProjectManagerWrapper("Unistream.Processing.Operations", Path.Combine(tempPath, "packages"), tempPath, new ConsoleLogger(), PackageRepositoryFactory.Default.CreateRepository(@"d:\AppsTest\Repo30"));
//            applicationProjectManager.InstallPackage("Unistream.Processing.Operations", new SemanticVersion("1.0.1.23"));
            var installedPackage=GetInstalledPackage(applicationProjectManager, "Unistream.Processing.Operations");
            var update = applicationProjectManager.GetUpdate(installedPackage);

            Console.WriteLine("UPGRADING FROM {0} TO {1}",installedPackage.Version,update.First().Version);
            Upgrade(applicationProjectManager, "Unistream.Processing.Operations");
        }

        public void Upgrade(ProjectManagerWrapper projectManager, string packageId)
        {
            var installedPackage = GetInstalledPackage(projectManager, packageId);
            var update = projectManager.GetUpdate(installedPackage).First();
            projectManager.UpdatePackage(update);
         
        }

        private static IPackage GetInstalledPackage(ProjectManagerWrapper projectManager, string packageId)
        {
            var package = (from p in projectManager.GetInstalledPackages(packageId)
                           where p.Id == packageId
                           select p).ToList<IPackage>().FirstOrDefault<IPackage>();
            if (package == null)
            {
                throw new InvalidOperationException(string.Format("The package for package ID '{0}' is not installed in this website. Copy the package into the App_Data/packages folder.", packageId));
            }
            return package;
        }

    }
}