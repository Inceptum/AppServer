using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;
using NuGet.Resources;

namespace Inceptum.AppServer.NuGetAppInstaller
{
    internal class NugetApplicationRepository : IApplicationRepository
    {
        public IEnumerable<ApplicationInfo> GetAvailableApps()
        {
            IPackageRepository appsRepo = PackageRepositoryFactory.Default.CreateRepository(@"d:\AppsTest\Repo23");
            var packages = from p in appsRepo.GetPackages() where p.Tags != null && p.Tags.Contains("Inceptum.AppServer.Application") orderby p.Id select p;
            return packages.ToArray().Select(package => new ApplicationInfo()
            {
                ApplicationId = package.Id,
                Vendor = string.Join(", ", package.Authors),
                Version = package.Version.Version
            });
        }

        public string Install(string path, ApplicationInfo application)
        {
            string installPath = Path.Combine(path, "bin");
            var projectManager = new ApplicationProjectManager(installPath, @"d:\AppsTest\Repo23");
            projectManager.InstallPackage(application.ApplicationId, new SemanticVersion(application.Version));
            return installPath;
        }

        public string Upgrade(string path, ApplicationInfo application)
        {
            string installPath = path;
            var projectManager = new ApplicationProjectManager(installPath, @"d:\AppsTest\Repo30");

            string packageId=application.ApplicationId;
            var package = (from p in projectManager.GetInstalledPackages(packageId)
                           where p.Id == packageId && p.Version == new SemanticVersion(application.Version)
                           select p).ToList<IPackage>().FirstOrDefault<IPackage>();
            if(package==null)
                throw new InvalidOperationException("");
            projectManager.UpdatePackage(package);
            return installPath;
        }
    }



    class MyProjectManager : ProjectManager
    {
        public MyProjectManager(IPackageRepository sourceRepository, IPackagePathResolver pathResolver, IProjectSystem project, IPackageRepository localRepository) : base(sourceRepository, pathResolver, project, localRepository)
        {
        }

        protected override void ExtractPackageFilesToProject(IPackage package)
        {
            base.ExtractPackageFilesToProject(package); 
            IEnumerable<IPackageFile> configItems;
            Project.TryGetCompatibleItems(package.GetFiles("config"), out configItems);
            var configFiles = configItems.ToArray();
            if (!configFiles.Any() && package.GetFiles("config").Any())
            {
                // for portable framework, we want to show the friendly short form (e.g. portable-win8+net45+wp8) instead of ".NETPortable, Profile=Profile104".
                FrameworkName targetFramework = Project.TargetFramework;
                string targetFrameworkString = /*targetFramework.IsPortableFramework()*/targetFramework != null && ".NETPortable".Equals(targetFramework.Identifier, StringComparison.OrdinalIgnoreCase)
                                                    ? VersionUtility.GetShortFrameworkName(targetFramework)
                                                    : targetFramework != null ? targetFramework.ToString() : null;

                throw new InvalidOperationException(
                           String.Format(CultureInfo.CurrentCulture,
                           NuGetResources.UnableToFindCompatibleItems, package.GetFullName(), targetFrameworkString));
            }

            if (configFiles.Any())
            {
                Logger.Log(MessageLevel.Debug, ">> {0} are being added from '{1}'{2}", "config files",
                    Path.GetDirectoryName(configFiles[0].Path), GetTargetFrameworkLogString(configFiles[0].TargetFramework));
            }
            // Add config files
            Project.AddFiles(configFiles, new Dictionary<FileTransformExtensions, IPackageFileTransformer>());
        }
        public static string GetTargetFrameworkLogString(FrameworkName targetFramework)
        {
            return (targetFramework == null || targetFramework == VersionUtility.EmptyFramework) ? "(not framework-specific)" : String.Empty;
        }
    }
    public class ApplicationProjectManager : ILogger
    {
        private readonly MyProjectManager m_ProjectManager;
        public ApplicationProjectManager(string applicationRoot, params string[] remoteSources)
        {
            string webRepositoryDirectory = GetRepositoryDirectory(applicationRoot);
            var sourceRepository = new AggregateRepository(remoteSources.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray());
            var pathResolver = new DefaultPackagePathResolver(webRepositoryDirectory);
            var localRepository = PackageRepositoryFactory.Default.CreateRepository(webRepositoryDirectory);
            IProjectSystem project = new ApplicationProjectSystem(applicationRoot) { Logger = this };
            m_ProjectManager = new MyProjectManager(sourceRepository, pathResolver, project, localRepository) { Logger = this };

        }
        // Properties
        public IPackageRepository LocalRepository
        {
            get
            {
                return m_ProjectManager.LocalRepository;
            }
        }

        public IPackageRepository SourceRepository
        {
            get
            {
                return m_ProjectManager.SourceRepository;
            }
        }

        private string GetRepositoryDirectory(string applicationRoot)
        {
            return Path.Combine(applicationRoot,  "packages");
        }

        public void InstallPackage(string packageId,SemanticVersion version)
        {
            m_ProjectManager.AddPackageReference(packageId, ignoreDependencies: false, allowPrereleaseVersions: true, version: version);
        }

        public void UninstallPackage(IPackage package, bool removeDependencies)
        {
            m_ProjectManager.RemovePackageReference(package.Id,forceRemove: false, removeDependencies: removeDependencies);
        }
        public void UpdatePackage(IPackage package)
        {
            m_ProjectManager.UpdatePackageReference(
                        package.Id,
                        package.Version,
                        updateDependencies: true,
                        allowPrereleaseVersions: true);
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
          /*  throw new Exception(message);
            Console.WriteLine(message);*/
            return FileConflictResolution.Ignore;
        }


        public IQueryable<IPackage> GetInstalledPackages(string searchTerms)
        {
            return GetPackages(LocalRepository, searchTerms);
        }

        internal static IQueryable<IPackage> GetPackages(IPackageRepository repository, string searchTerm)
        {
            return GetPackages(repository.GetPackages(), searchTerm);
        }


        internal static IQueryable<IPackage> GetPackages(IQueryable<IPackage> packages, string searchTerm)
        {
            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                packages = packages.Find(searchTerm);
            }
            return packages;
        }
        public IPackage GetUpdate(IPackage package)
        {
            return SourceRepository.GetUpdates(
                LocalRepository.GetPackages(),
                includePrerelease: true,
                includeAllVersions: true)
            .LastOrDefault(p => (package.Id == p.Id));
        }
        public void Log(MessageLevel level, string message, params object[] args)
        {
            Console.WriteLine(level+": "+message, args);
        }
    }
}