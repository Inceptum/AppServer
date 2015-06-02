using System.Linq;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    public class ProjectManagerWrapper : ILogger
    {
        private readonly ApplicationProjectManager m_ProjectManager;
        private readonly Castle.Core.Logging.ILogger m_Logger;

        public ProjectManagerWrapper(string packageId,string sharedRepositoryDir,string applicationRoot, Castle.Core.Logging.ILogger logger, IPackageRepository dependenciesRepository)
        {
            m_Logger = logger;

            var sharedRepositoryFileSystem = new PhysicalFileSystem(sharedRepositoryDir);
            var pathResolver = new DefaultPackagePathResolver(sharedRepositoryFileSystem);
            var localSharedRepository=new SharedPackageRepository(pathResolver, sharedRepositoryFileSystem, sharedRepositoryFileSystem);

            IProjectSystem project = new ApplicationProjectSystem(applicationRoot) { Logger = this };
            var referenceRepository = new PackageReferenceRepository(project, packageId, localSharedRepository);
            m_ProjectManager = new ApplicationProjectManager(packageId,dependenciesRepository, pathResolver, project, referenceRepository, localSharedRepository) { Logger = this };
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
                includePrerelease: false,
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