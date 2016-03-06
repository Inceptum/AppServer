using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    public class ProjectManagerWrapper : ILogger
    {
        private readonly Castle.Core.Logging.ILogger m_Logger;
        private readonly ApplicationProjectManager m_ProjectManager;
        private readonly bool m_AllowPrereleaseVersions;
        private readonly AggregateRepository m_Repository;
        private string m_PackageId;

        public ProjectManagerWrapper(string packageId, string sharedRepositoryDir, string applicationRoot, Castle.Core.Logging.ILogger logger, IPackageRepository dependenciesRepository, DependencyVersion versionStrategy, bool allowPrereleaseVersions)
        {
            m_PackageId = packageId;
            m_Logger = logger;
            m_AllowPrereleaseVersions = allowPrereleaseVersions;

            var sharedRepositoryFileSystem = new PhysicalFileSystem(sharedRepositoryDir);
            var pathResolver = new DefaultPackagePathResolver(sharedRepositoryFileSystem);
            var localSharedRepository = new SharedPackageRepository(pathResolver, sharedRepositoryFileSystem, sharedRepositoryFileSystem);
            

            IProjectSystem project = new ApplicationProjectSystem(applicationRoot) {Logger = this};
            var referenceRepository = new PackageReferenceRepository(project, packageId, localSharedRepository);

            m_Repository = new AggregateRepository(new[] { localSharedRepository, dependenciesRepository });
            m_ProjectManager = new ApplicationProjectManager(packageId, m_Repository, pathResolver, project, referenceRepository,
                localSharedRepository)
            {
                DependencyVersion = versionStrategy,
                Logger = this
            };
        }

        public IPackageRepository LocalRepository
        {
            get { return m_ProjectManager.LocalRepository; }
        }

        public IPackageRepository SourceRepository
        {
            get { return m_ProjectManager.SourceRepository; }
        }

        public FileConflictResolution ResolveFileConflict(string message)
        {
            return FileConflictResolution.Ignore;
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

        public void InstallPackage(SemanticVersion version)
        {
            var packagesConfig = m_Repository.FindPackage(m_PackageId, version).GetFiles().FirstOrDefault(f => f.Path.ToLower() == "config\\packages.config");
            if (packagesConfig!=null )
            {

                m_ProjectManager.AddPackageReference(m_PackageId, ignoreDependencies: true, allowPrereleaseVersions: m_AllowPrereleaseVersions, version: version);
                var file = new PackageReferenceFile(new InMemoryPackagesConfigFileSystem(packagesConfig.GetStream()),"packages.config");
                var packageReferences = GetPackageReferences(file, true);
                foreach (var reference in packageReferences)
                {
                    m_ProjectManager.AddPackageReference(reference.Id, ignoreDependencies: true, allowPrereleaseVersions: m_AllowPrereleaseVersions, version: reference.Version);
                }

                //TODO: investigate parallel installation. Need to synchronize shared repository and packages.config access
/*
                var tasks = packageReferences.Select(reference =>
                           Task.Factory.StartNew(() =>
                           {
                               m_ProjectManager.AddPackageReference(reference.Id, ignoreDependencies: true, allowPrereleaseVersions: m_AllowPrereleaseVersions, version: reference.Version);
                           })).ToArray();

                Task.WaitAll(tasks);*/

                //InstallSatellitePackages(fileSystem, satellitePackages);
            }
            else
            {
                m_Logger.WarnFormat("packages.config is not found in config folder of application package. It is recommended to provide packages.config as part of application package for appserver to install exactly same  dependencies versions, applications was built and tested with. Will attempt to install dependencies following from what is defined in application package nuspec");
                m_ProjectManager.AddPackageReference(m_PackageId, ignoreDependencies: false, allowPrereleaseVersions: m_AllowPrereleaseVersions, version: version);
            }
        }


        public static ICollection<PackageReference> GetPackageReferences(PackageReferenceFile configFile, bool requireVersion)
        {
            if (configFile == null)
            {
                throw new ArgumentNullException("configFile");
            }

            var packageReferences = configFile.GetPackageReferences(requireVersion).ToList();
            foreach (var package in packageReferences)
            {
                // GetPackageReferences returns all records without validating values. We'll throw if we encounter packages
                // with malformed ids / Versions.
                if (String.IsNullOrEmpty(package.Id))
                {
                    throw new InvalidDataException("Invalid package reference:"+configFile.FullPath);
                }
                if (requireVersion && (package.Version == null))
                {
                    throw new InvalidDataException(String.Format("Package reference has invalid version {0}", package.Id));
                }
            }

            return packageReferences;
        }

        public void Uninstall()
        {
            foreach (var package in LocalRepository.GetPackages())
            {
                m_ProjectManager.RemovePackageReference(package.Id, true, false);                
            }

        }

        public void UpdatePackage(IPackage package)
        {
            m_ProjectManager.UpdatePackageReference(
                package.Id,
                package.Version, true, true);
        }

        public IQueryable<IPackage> GetInstalledPackages(string searchTerm)
        {
            var packages = LocalRepository.GetPackages();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                packages = packages.Find(searchTerm);
            }

            return packages;
        }

 
        public SemanticVersion GetInstalledApplicationVersion()
        {
            var packages = LocalRepository.GetPackages();
                
            var package = packages.Find(m_PackageId).FirstOrDefault();

            return package==null?null:package.Version;
        }

        public IPackage[] GetUpdate(IPackage package)
        {
            return SourceRepository.GetUpdates(
                LocalRepository.GetPackages(), false, true)
                .Where(p => (package.Id == p.Id)).ToArray();
        }
    }
 
}