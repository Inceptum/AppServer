using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Inceptum.AppServer.Configuration;
using Newtonsoft.Json.Linq;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal class NugetApplicationRepository : IApplicationRepository
    {
        private readonly Castle.Core.Logging.ILogger m_Logger;
        private readonly string m_LocalSharedRepository;
        private readonly IPackageRepository m_ApplicationRepository;
        private readonly IPackageRepository m_DependenciesRepository;

        public NugetApplicationRepository(Castle.Core.Logging.ILogger logger, IManageableConfigurationProvider configurationProvider)
        {
            m_Logger = logger;
            var bundleString = configurationProvider.GetBundle("AppServer", "server.host", "{environment}", "{machineName}");
            dynamic bundle = JObject.Parse(bundleString).SelectToken("nuget");
            var applicationRepository = (string)bundle.applicationRepository;
            var dependenciesRepositories = ((JArray)bundle.dependenciesRepositories).Select(t => t.ToString()).Select(getRepositoryPath).ToArray(); ;

            m_ApplicationRepository = PackageRepositoryFactory.Default.CreateRepository(getRepositoryPath(applicationRepository));
            IPackageRepository[] dependencyRepositories = dependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();
 
            m_DependenciesRepository = new AggregateRepository(
                new[] { m_ApplicationRepository }.Concat(dependencyRepositories)
                );

            m_LocalSharedRepository = Path.GetFullPath("packages\\");
        }
        private string getRepositoryPath(string repository)
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

        public IEnumerable<ApplicationInfo> GetAvailableApps()
        {
            var packages = from p in m_ApplicationRepository.GetPackages() where p.Tags != null && p.Tags.Contains("Inceptum.AppServer.Application") orderby p.Id select p;
            return packages.ToArray().Select(package => new ApplicationInfo()
            {
                ApplicationId = package.Id,
                Vendor = string.Join(", ", package.Authors),
                Version = package.Version.Version,
                Description = package.Description 
            });
        }

        public void Install(string path, ApplicationInfo application)
        {

            string installPath = path;
            Version installedVersion=null;
            var versionFile = Path.Combine(installPath, "version");
            var projectManager = new ProjectManagerWrapper(application.ApplicationId,m_LocalSharedRepository, installPath, m_Logger, m_DependenciesRepository);


            if (File.Exists(versionFile))
            {
                installedVersion = Version.Parse(File.ReadAllText(versionFile));
            }

            if (installedVersion == application.Version) 
                return;

            if (installedVersion != null)
            {
                string packageId = application.ApplicationId;
                var package = (from p in projectManager.GetInstalledPackages(packageId)
                    where p.Id == packageId && p.Version == new SemanticVersion(installedVersion)
                    select p).ToList<IPackage>().FirstOrDefault<IPackage>();
                if (package != null)
                    projectManager.UninstallPackage(package, true);
                else
                {
                    m_Logger.WarnFormat("Failed to find package {0} version {1} from which instance was installed. Will clean up folder manually instead of package uninstall ",packageId, installedVersion);
                    cleanUpInstallFolder(installPath);
                }
            }
            else
            {
                cleanUpInstallFolder(installPath);
            }
            projectManager.InstallPackage(application.ApplicationId, new SemanticVersion(application.Version));
            File.WriteAllText(versionFile, application.Version.ToString());
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

        public void Upgrade(string path, ApplicationInfo application)
        {
            Install(path,application);
        }

        public string Name { get { return "nuget"; }}
    }
}