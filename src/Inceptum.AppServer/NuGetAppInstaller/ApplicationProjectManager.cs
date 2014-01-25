using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;

namespace Inceptum.AppServer.NuGetAppInstaller
{
    internal class NugetApplicationRepository : IApplicationRepository
    {
        private Castle.Core.Logging.ILogger m_Logger;
        private string m_SharedRepository;

        public NugetApplicationRepository(Castle.Core.Logging.ILogger logger, string sharedRepository)
        {
            m_Logger = logger;
            m_SharedRepository = sharedRepository;
        }

        public IEnumerable<ApplicationInfo> GetAvailableApps()
        {
            IPackageRepository appsRepo = PackageRepositoryFactory.Default.CreateRepository(@"d:\AppsTest\Repo");
            var packages = from p in appsRepo.GetPackages() where p.Tags != null && p.Tags.Contains("Inceptum.AppServer.Application") orderby p.Id select p;
            return packages.ToArray().Select(package => new ApplicationInfo()
            {
                ApplicationId = package.Id,
                Vendor = string.Join(", ", package.Authors),
                Version = package.Version.Version,
                Description = package.Description 
            });
        }

        public string Install(string path, ApplicationInfo application)
        {

            string installPath = path;
            Version installedVersion=null;
            var versionFile = Path.Combine(installPath, "version");
            var projectManager = new ApplicationProjectManager(m_SharedRepository, installPath, m_Logger, @"d:\AppsTest\Repo");


            if (File.Exists(versionFile))
            {
                installedVersion = Version.Parse(File.ReadAllText(versionFile));
            }
          
            if (installedVersion != application.Version)
            {
                if (installedVersion != null)
                {
                    string packageId = application.ApplicationId;
                    var package = (from p in projectManager.GetInstalledPackages(packageId)
                                   where p.Id == packageId && p.Version == new SemanticVersion(installedVersion)
                                   select p).ToList<IPackage>().FirstOrDefault<IPackage>();
                    projectManager.UninstallPackage(package, true);
                }
                projectManager.InstallPackage(application.ApplicationId, new SemanticVersion(application.Version));
                File.WriteAllText(versionFile, application.Version.ToString());
            }


            return installPath;
        }

        public string Upgrade(string path, ApplicationInfo application)
        {
            string installPath = path;
            var projectManager = new ApplicationProjectManager(m_SharedRepository, installPath, m_Logger, @"d:\AppsTest\Repo");

            string packageId=application.ApplicationId;
            var package = (from p in projectManager.GetInstalledPackages(packageId)
                           where p.Id == packageId && p.Version == new SemanticVersion(application.Version)
                           select p).ToList<IPackage>().FirstOrDefault<IPackage>();
            if(package==null)
                throw new InvalidOperationException("");
            var update = projectManager.GetUpdate(package).FirstOrDefault(p => p.Version==new SemanticVersion(application.Version));
            if (update == null)
                throw new InvalidOperationException("update not found");

            projectManager.UpdatePackage(update);
            return installPath;
        }

        public string Name { get { return "nuget"; }}
    }


    public class ApplicationProjectManager : ILogger
    {
        private readonly MyProjectManager m_ProjectManager;
        private Castle.Core.Logging.ILogger m_Logger;

        public ApplicationProjectManager(string sharedRepository,string applicationRoot, Castle.Core.Logging.ILogger logger, params string[] remoteSources)
        {
            m_Logger = logger;
            string sharedRepositoryDirectory = sharedRepository;
            var sourceRepository = new AggregateRepository(remoteSources.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray());
/*
            var pathResolver = new DefaultPackagePathResolver(applicationRoot);
            var localRepository = PackageRepositoryFactory.Default.CreateRepository(webRepositoryDirectory);
*/

         

            var sharedRepositoryFileSystem = new PhysicalFileSystem(sharedRepositoryDirectory);
            var pathResolver = new DefaultPackagePathResolver(sharedRepositoryFileSystem);
            var localSharedRepository=new SharedPackageRepository(pathResolver, sharedRepositoryFileSystem, sharedRepositoryFileSystem);


            IProjectSystem project = new ApplicationProjectSystem(applicationRoot) { Logger = this };
            var referenceRepository = new PackageReferenceRepository(project, localSharedRepository);
            m_ProjectManager = new MyProjectManager(sourceRepository, pathResolver, project, referenceRepository, localSharedRepository) { Logger = this };



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

   
        public void InstallPackage(string packageId,SemanticVersion version)
        {
            //m_ProjectManager.AddPackageReference(packageId, version);
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
        public IPackage[] GetUpdate(IPackage package)
        {
            return SourceRepository.GetUpdates(
                LocalRepository.GetPackages(),
                includePrerelease: true,
                includeAllVersions: true)
            .Where(p => (package.Id == p.Id)).ToArray();
        }
        public void Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                case MessageLevel.Debug:
                    m_Logger.DebugFormat(message, args);
                    break;
                case MessageLevel.Error:
                    m_Logger.ErrorFormat(message, args);
                    break;
                case MessageLevel.Info:
                    m_Logger.InfoFormat(message, args);
                    break;
                case MessageLevel.Warning:
                    m_Logger.WarnFormat(message, args);
                    break;
            }
        }
    }
}