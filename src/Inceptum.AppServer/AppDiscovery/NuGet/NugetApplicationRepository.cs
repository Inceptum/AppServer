using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NuGet;
using ILogger = Castle.Core.Logging.ILogger;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal class NugetApplicationRepository : IApplicationRepository
    {
        private readonly IPackageRepository m_ApplicationRepository;
        private readonly IPackageRepository m_DependenciesRepository;
        private readonly string m_LocalSharedRepository;
        private readonly ILogger m_Logger;
        private readonly NugetApplicationRepositoryConfiguration m_Configuration;

        public NugetApplicationRepository(ILogger logger, NugetApplicationRepositoryConfiguration configuration,string cacheLocation=null)
        {
            m_Logger = logger;
            m_LocalSharedRepository = cacheLocation??Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), @"packages\");
            m_Configuration = configuration;
            m_Logger.InfoFormat("Nuget cache location: {0}",m_LocalSharedRepository);

            m_ApplicationRepository = PackageRepositoryFactory.Default.CreateRepository(getRepositoryPath(m_Configuration.ApplicationRepository));

            var dependenciesRepositories = configuration.DependenciesRepositories.Select(getRepositoryPath).ToArray();
            var dependencyRepositories = dependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();

            m_DependenciesRepository = new AggregateRepository(new[] {m_ApplicationRepository}.Concat(dependencyRepositories)) {ResolveDependenciesVertically = true};
        }

        public IEnumerable<ApplicationInfo> GetAvailableApps()
        {
            var packages = from p in m_ApplicationRepository.GetPackages() where p.Tags != null && p.Tags.Contains("Inceptum.AppServer.Application") orderby p.Id select p;
            return packages.ToArray().Select(package => new ApplicationInfo
            {
                ApplicationId = package.Id,
                Vendor = string.Join(", ", package.Authors),
                Version = package.Version.Version,
                Description = package.Description
            });
        }

        public void Install(string path, ApplicationInfo application)
        {
            var versionFile = Path.Combine(path, "version");
            Version installedVersion = null;

            if (File.Exists(versionFile))
            {
                installedVersion = Version.Parse(File.ReadAllText(versionFile));
            }

            if (installedVersion == application.Version)
            {
                return;
            }

            var dependencyVersion = m_Configuration.DependencyVersion ?? DependencyVersion.Highest;
            var allowPrereleaseVersions = m_Configuration.AllowPrereleaseVersions ?? true;

            var projectManager = new ProjectManagerWrapper(application.ApplicationId, m_LocalSharedRepository, path, m_Logger, m_DependenciesRepository, dependencyVersion, allowPrereleaseVersions);
            if (installedVersion != null)
            {
                var packageId = application.ApplicationId;

                try
                {
                    projectManager.Uninstall();
                }
                catch (Exception e)
                {
                    m_Logger.WarnFormat(e,"Failed to uninstall previous version of {0}. Will clean up folder manually instead of package uninstall ", packageId);
                    cleanUpInstallFolder(path);
                }
            }
            else
            {
                cleanUpInstallFolder(path);
            }

              
            if (File.Exists(versionFile))
                    File.Delete(versionFile);
            
            projectManager.InstallPackage(new SemanticVersion(application.Version));

            File.WriteAllText(versionFile, application.Version.ToString());
        }

        public void Upgrade(string path, ApplicationInfo application)
        {
            Install(path, application);
        }

        public string Name
        {
            get { return "nuget"; }
        }

        private static string getRepositoryPath(string repository)
        {
            if (!Uri.IsWellFormedUriString(repository, UriKind.RelativeOrAbsolute))
            {
                repository = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, repository);
            }

            var uri = new Uri(repository);
            if (uri.IsFile && !Directory.Exists(uri.LocalPath))
            {
                throw new Exception(string.Format("Failed to find folder at '{0}'", uri.LocalPath));
            }
            return repository;
        }

        private void cleanUpInstallFolder(string installPath)
        {
            m_Logger.WarnFormat("Cleaning up install folder '{0}'", installPath);
            var binFolder = Path.GetFullPath(Path.Combine(installPath, "bin"));
            if (Directory.Exists(binFolder))
            {
                m_Logger.WarnFormat("Deleting {0} folder", binFolder);
                Directory.Delete(binFolder, true);
            }

            var contentFolder = Path.GetFullPath(Path.Combine(installPath, "content"));
            if (Directory.Exists(contentFolder))
            {
                m_Logger.WarnFormat("Deleting {0} folder", contentFolder);
                Directory.Delete(contentFolder, true);
            }

            var packagesConfig = Path.GetFullPath(Path.Combine(installPath, "packages.config"));
            if (File.Exists(packagesConfig))
            {
                m_Logger.WarnFormat("Deleting {0}", packagesConfig);
                File.Delete(packagesConfig);
            }
        }
    }
}