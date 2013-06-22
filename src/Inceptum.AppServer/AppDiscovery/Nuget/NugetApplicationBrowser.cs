using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using NuGet;
using OpenWrap.Configuration;

namespace Inceptum.AppServer.AppDiscovery.Nuget
{
    public class NugetApplicationBrowser : IApplicationBrowser
    {
        internal static readonly FrameworkName NET40= new FrameworkName(".NETFramework,Version=v4.5");
        private readonly string m_ApplicationRepository;
        private readonly string[] m_DependenciesRepositories;

        public NugetApplicationBrowser(string applicationRepository, params string[] dependenciesRepositories)
        {
            m_DependenciesRepositories = dependenciesRepositories.Select(
                r => Directory.Exists(r)?Path.GetFullPath(r):r).ToArray();
            m_ApplicationRepository = Directory.Exists(applicationRepository) ? Path.GetFullPath(applicationRepository) : applicationRepository;
        }

        private IEnumerable<IPackage> getDependencies(IPackage package, IPackageRepository repository)
        {
            return package.GetCompatiblePackageDependencies(NET40).SelectMany(d =>
                                   {
                                       var dependency = repository.ResolveDependency(d, false, false);
                                       return getDependencies(dependency, repository);
                                   }).Concat(new []{package});
        }

        private IEnumerable<IPackageAssemblyReference> getAssemblies(IPackage package)
        {
            IEnumerable<IPackageAssemblyReference> refs;
            if (!VersionUtility.TryGetCompatibleItems(NET40, package.AssemblyReferences, out refs))
            {
                throw new ConfigurationErrorsException(string.Format("Failed to load package {0} since it is not compartible with .Net 4.0",package.Id));
            }
            return refs;
        }

        public string Name
        {
            get { return "Nuget"; }
        }

        public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {
            IPackageRepository appsRepo =PackageRepositoryFactory.Default.CreateRepository(m_ApplicationRepository);
            IPackageRepository[] dependencyRepositories = m_DependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();
            var dependencyRepo = new AggregateRepository(
                    new []{appsRepo}.Concat(dependencyRepositories)
                );

            var appsRepoManager = new PackageManager(appsRepo,  @".\NugetLoaclRepo");
            var manager = new PackageManager(dependencyRepo, @".\NugetLoaclRepo");
            var packages = from p in appsRepoManager.SourceRepository.GetPackages() where p.Tags!=null && p.Tags.Contains("Inceptum.AppServer.Applicaton") orderby p.Id select new PackageWrapper(p);

            var res = new List<HostedAppInfo>();

            foreach (var package in packages)
            {
                //TODO: check compartability of AppServer version and package.SdkVersion
                manager.InstallPackage(package, false, true);
                var assembliesToLoad =
                    from p in getDependencies(package, manager.LocalRepository).Distinct()
                    from a in getAssemblies(p)
                    select new { name = new AssemblyName(a.Name), path = Path.Combine(manager.LocalRepository.Source ,manager.PathResolver.GetPackageDirectory(p), a.Path) };


                IEnumerable<string> packageAssemblies = getAssemblies(package)
                    .Select(a => Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(package), a.Path));
                string appConfig = package.GetFiles().Where(f => f.Path.ToLower() == @"config\app.config")
                    .Select(c=>Path.Combine(manager.LocalRepository.Source ,manager.PathResolver.GetPackageDirectory(package), c.Path))
                    .Select(Path.GetFullPath).FirstOrDefault();

                res.Add(
                    new HostedAppInfo(
                        package.Id, 
                        string.Join(", ", package.Authors),
                        package.Version.Version,
                        getAppType(packageAssemblies),
                        assembliesToLoad.ToDictionary(a =>
                            {
                                var assembly = CeceilExtencions.TryReadAssembly(a.path);
                                return new AssemblyName(assembly.Name.Name);
                            },a=>a.path)
                
                        ){ConfigFile = appConfig,Description = package.Description}
                    );
            }


            return res;
        }

        private string getAppType(IEnumerable<string> packageAssemblies)
 
        {
            foreach (var assemblyPath in packageAssemblies)
            {

                var assembly = CeceilExtencions.TryReadAssembly(assemblyPath);
                var attribute = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == typeof(HostedApplicationAttribute).FullName);
                if (attribute == null)
                    continue;
                var appType = assembly.MainModule.Types.FirstOrDefault(t => t.Interfaces.Any(i => i.FullName == typeof(IHostedApplication).FullName));
                if (appType == null)
                    continue;
                return appType.FullName + ", " + assembly.FullName;
            }
        
            throw new ConfigurationErrorsException("Application class not found");
        }
    }


    /// <summary>
    /// Filters out Inceptum.AppServer.Sdk dependency
    /// </summary>
    class PackageWrapper:IPackage
    {
        private readonly IPackage m_Package;

        public PackageWrapper(IPackage package)
        {
            m_Package = package;
        }

        public string Id
        {
            get { return m_Package.Id; }
        }

        public SemanticVersion Version
        {
            get { return m_Package.Version; }
        }

        public string Title
        {
            get { return m_Package.Title; }
        }

        public IEnumerable<string> Authors
        {
            get { return m_Package.Authors; }
        }

        public IEnumerable<string> Owners
        {
            get { return m_Package.Owners; }
        }

        public Uri IconUrl
        {
            get { return m_Package.IconUrl; }
        }

        public Uri LicenseUrl
        {
            get { return m_Package.LicenseUrl; }
        }

        public Uri ProjectUrl
        {
            get { return m_Package.ProjectUrl; }
        }

        public bool RequireLicenseAcceptance
        {
            get { return m_Package.RequireLicenseAcceptance; }
        }

        public string Description
        {
            get { return m_Package.Description; }
        }

        public string Summary
        {
            get { return m_Package.Summary; }
        }

        public string ReleaseNotes
        {
            get { return m_Package.ReleaseNotes; }
        }

        public string Language
        {
            get { return m_Package.Language; }
        }

        public string Tags
        {
            get { return m_Package.Tags; }
        }

        public string Copyright
        {
            get { return m_Package.Copyright; }
        }

        public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies
        {
            get { return m_Package.FrameworkAssemblies; }
        }

        public IVersionSpec SdkVersion
        {
            get
            {
                return (from dependencySet in m_Package.DependencySets
                        where dependencySet.TargetFramework == NugetApplicationBrowser.NET40
                        from packageDependency in dependencySet.Dependencies
                        where packageDependency.Id == "Inceptum.AppServer.Sdk"
                        select packageDependency.VersionSpec).FirstOrDefault();
            }
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get
            {
                return m_Package.DependencySets
                                .Select(ds => new PackageDependencySet(
                                                  ds.TargetFramework,
                                                  ds.Dependencies
                                                    .Where(dependency => dependency.Id != "Inceptum.AppServer.Sdk")
                                                  )
                    ).Where(ds=>ds.Dependencies.Any());
            }
        }

        public Uri ReportAbuseUrl
        {
            get { return m_Package.ReportAbuseUrl; }
        }

        public int DownloadCount
        {
            get { return m_Package.DownloadCount; }
        }

        public IEnumerable<IPackageFile> GetFiles()
        {
            return m_Package.GetFiles();
        }

        public IEnumerable<FrameworkName> GetSupportedFrameworks()
        {
            return m_Package.GetSupportedFrameworks();
        }

        public Stream GetStream()
        {
            return m_Package.GetStream();
        }

        public bool IsAbsoluteLatestVersion
        {
            get { return m_Package.IsAbsoluteLatestVersion; }
        }

        public bool IsLatestVersion
        {
            get { return m_Package.IsLatestVersion; }
        }

        public bool Listed
        {
            get { return m_Package.Listed; }
        }

        public DateTimeOffset? Published
        {
            get { return m_Package.Published; }
        }

        public IEnumerable<IPackageAssemblyReference> AssemblyReferences
        {
            get { return m_Package.AssemblyReferences; }
        }
    }
}