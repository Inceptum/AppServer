using System;
using System.IO;
using System.Linq;
using Inceptum.AppServer.NuGetAppInstaller;
using NuGet;
using NUnit.Framework;

using Path=System.IO.Path;
namespace Inceptum.AppServer.Tests.NuGetInstaller
{
    [TestFixture]
    public class ApplicationProjectManagerTests
    {
        [Test]
        public void InstallTest()
        {
         /*   string tempPath =  Path.GetTempFileName();
            File.Delete(tempPath);
            Directory.CreateDirectory(tempPath);*/

            var tempPath = @"d:\AppsTest\operations";
            if(Directory.Exists(tempPath))
                Directory.Delete(tempPath,true);
                
            Directory.CreateDirectory(tempPath);
            Console.WriteLine(tempPath);
            var applicationProjectManager = new ApplicationProjectManager(tempPath, @"d:\AppsTest\Repo23");
            applicationProjectManager.InstallPackage("Unistream.Processing.Operations", new SemanticVersion("1.0.1.23"));
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
            var applicationProjectManager = new ApplicationProjectManager(tempPath, @"d:\AppsTest\Repo30");
//            applicationProjectManager.InstallPackage("Unistream.Processing.Operations", new SemanticVersion("1.0.1.23"));
            var installedPackage=GetInstalledPackage(applicationProjectManager, "Unistream.Processing.Operations");
            var update = applicationProjectManager.GetUpdate(installedPackage);

            Console.WriteLine("UPGRADING FROM {0} TO {1}",installedPackage.Version,update.Version);
            Upgrade(applicationProjectManager, "Unistream.Processing.Operations");
        }

        public void Upgrade(ApplicationProjectManager projectManager, string packageId)
        {
            var installedPackage = GetInstalledPackage(projectManager, packageId);
            var update = projectManager.GetUpdate(installedPackage);
            projectManager.UpdatePackage(update);
         
        }

        private static IPackage GetInstalledPackage(ApplicationProjectManager projectManager, string packageId)
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