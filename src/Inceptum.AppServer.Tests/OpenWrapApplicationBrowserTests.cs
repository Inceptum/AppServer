using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.AppDiscovery.Openwrap;
using NUnit.Framework;
using OpenFileSystem.IO.FileSystems.Local.Win32;
using OpenWrap.PackageManagement;
using OpenWrap.PackageModel;
using OpenWrap.Repositories;
using OpenWrap.Runtime;
using OpenWrap.Services;

namespace Inceptum.AppServer.Tests
{
    [TestFixture]
    [Ignore]
    public class OpenWrapApplicationBrowserTests
    {
        [Test]
        public void GetAvailabelApps()
        {
            var browser = new OpenWrapApplicationBrowser(@"e:\Dropbox\WORK\_GENERIC\openwrap\ibank\", @"d:\tmp\openwrap\repo\",null);
            foreach (var app in browser.GetAvailabelApps())
            {
                Console.WriteLine(app);
            }
        }


      /*  [Test]
        public void Test1()
        {

            IEnumerable<string> bootstrapPackagePaths = Enumerable.Empty<string>();
            const string projectPath = @"d:\tmp\openwrap\proj";
            if (projectPath != null)
            {
                EnsurePackagesUnzippedInRepository(projectPath);

                bootstrapPackagePaths = GetLatestPackagesForProjectRepository(packageNamesToLoad, projectPath);
                if (bootstrapPackagePaths.Count() >= packageNamesToLoad.Length) return bootstrapPackagePaths;
            }
            FileNotFoundException fileNotFound = null;
            try
            {
                bootstrapPackagePaths = GetLatestPackagesForSystemRepository(systemRepositoryPath, packageNamesToLoad);
            }
            catch (FileNotFoundException e)
            {
                fileNotFound = e;
            }
            if ((fileNotFound != null || bootstrapPackagePaths.Count() < packageNamesToLoad.Length) && remote.Enabled)
                return TryDownloadPackages(systemRepositoryPath, packageNamesToLoad, remote);
            if (bootstrapPackagePaths.Count() >= packageNamesToLoad.Length)
                return bootstrapPackagePaths;
            throw new ArgumentException("No package present.", fileNotFound);
        }

        static void EnsurePackagesUnzippedInRepository(string repositoryPath)
        {
            foreach (var extraction in from directory in GetSelfAndParents(repositoryPath)
                                       where directory.Exists
                                       let wrapDirectoryInfo = new DirectoryInfo(Path.Combine(directory.FullName, "wraps"))
                                       where wrapDirectoryInfo.Exists
                                       let cacheDirectory = EnsureSubFolderExists(wrapDirectoryInfo, "_cache")
                                       from wrapFile in wrapDirectoryInfo.GetFiles("*.wrap")
                                       let wrapName = Path.GetFileNameWithoutExtension(wrapFile.Name)
                                       where !string.IsNullOrEmpty(wrapName)
                                       let cacheFolderForWrap = new DirectoryInfo(Path.Combine(cacheDirectory.FullName, wrapName))
                                       where cacheFolderForWrap.Exists == false
                                       select new { wrapFile, cacheFolderForWrap })
            {
                extraction.cacheFolderForWrap.Create();
                using (var stream = extraction.wrapFile.OpenRead())
                    Extractor(stream, extraction.cacheFolderForWrap.FullName);
            }
        }

        static DirectoryInfo EnsureSubFolderExists(DirectoryInfo wrapDirectoryInfo, string subfolder)
        {
            var di = new DirectoryInfo(Path.Combine(wrapDirectoryInfo.FullName, subfolder));
            if (!di.Exists)
                di.Create();
            return di;
        }

        static IEnumerable<DirectoryInfo> GetSelfAndParents(string directoryPath)
        {
            var directory = new DirectoryInfo(directoryPath);
            do
            {
                yield return directory;
                directory = directory.Parent;
            } while (directory != null);
        }
*/

        [Test]
        public void Test()
        {
            var serviceRegistry = new ServiceRegistry();
            serviceRegistry.Initialize();
            var packageManager = ServiceLocator.GetService<IPackageManager>();

            var environment = new ExecutionEnvironment
                                  {
                                      Platform = (IntPtr.Size == 4) ? "x86" : "x64",
                                      Profile = (Environment.Version.Major >= 4) ? "net40" : "net35"
                                  };

            IPackageDescriptor descriptor = new PackageDescriptor(new[]
                                                                      {
                                                                          new GenericDescriptorEntry("name", "test"),
                                                                          new GenericDescriptorEntry("version","0.0.0.1"),
                                                                          new GenericDescriptorEntry("depends","Inceptum.Framework")
                                                                      });
            IPackageRepository projectRepository = new FolderRepository(new Win32Directory(@"d:\tmp\openwrap\repo\"));
            IPackageRepository remoteRepository = new FolderRepository(new Win32Directory(@"d:\tmp\openwrap\remote1\"));


            IPackageAddResult addProjectPackage =
                packageManager.AddSystemPackage(PackageRequest.Any("Inceptum.Framework"), new[] {remoteRepository},
                                                projectRepository);
            foreach (PackageOperationResult r in addProjectPackage)
            {
                Console.WriteLine(r.Success);
            }

            IEnumerable<IGrouping<string, Exports.IAssembly>> projectExports =
                packageManager.GetProjectExports<Exports.IAssembly>(descriptor, projectRepository, environment);
            foreach (var projectExport in projectExports)
            {
                Console.WriteLine(projectExport.Key + ":" + projectExport.First().File);
            }
/*
            packageManager.GetProjectExports<Exports.IAssembly>()
            /*            var env = new CurrentDirectoryEnvironment();
                        env.Initialize();#1#

            string uri = @"file://d:\tmp\openwrap\remote\";
            Preloader.RemoteInstall remoteInstall = Preloader.RemoteInstall.FromServer(uri,new FakeNotifier(),null,null,null);
            Preloader.LoadAssemblies(
                    Preloader.GetPackageFolders(
                        remoteInstall, null, @"d:\tmp\openwrap\repo\wraps", "Inceptum.Framework")
                    );

*/
/*
            var repo = new FolderRepository(new Win32Directory(@"e:\WORK\FINAM\Inceptum.Framework\ThirdPartyRepo"));
            ExecutionEnvironment environment = new ExecutionEnvironment();
            environment.Platform = (IntPtr.Size == 4) ? "x86" : "x64";
            environment.Profile = (Environment.Version.Major >= 4) ? "net40" : "net35";

            DefaultAssemblyExporter exporter = new DefaultAssemblyExporter();
            IPackage package = repo.PackagesByName.Select(x => x.First()).First().Load();


            foreach (var item in exporter.Items<Exports.IAssembly>(package, environment))
            {
                Console.WriteLine(item.Key + "   " + item.First().AssemblyName);
            }*/

            /*

                        IEnumerable<Exports.IFile> enumerable = repo.PackagesByName
                            .Select(x => x.OrderByDescending(y => y.Version).First())
                            .NotNull()
                            .SelectMany(x => x.Load().Content)
                            .SelectMany(i => i);

                        foreach (var file in enumerable)
                        {
                            Console.WriteLine(file.File);
                        }

            */
        }
    }

   
}
