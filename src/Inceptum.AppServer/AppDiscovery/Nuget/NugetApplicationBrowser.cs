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
        private readonly FrameworkName NET40= new FrameworkName(".NETFramework,Version=v4.5");
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

        public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {
            IPackageRepository appsRepo =new RepositoryWrapper(PackageRepositoryFactory.Default.CreateRepository(m_ApplicationRepository));
            IPackageRepository[] dependencyRepositories = m_DependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();
            var dependencyRepo = new AggregateRepository(
                    dependencyRepositories
                );

            var appsRepoManager = new PackageManager(appsRepo,  @".\NugetLoaclRepo");
            var manager = new PackageManager(dependencyRepo, @".\NugetLoaclRepo");

            IPackage[] packages = appsRepoManager.SourceRepository.GetPackages().OrderBy(p => p.Id).ToArray();


            var res = new List<HostedAppInfo>();

            foreach (var package in packages)
            {
                //manager.InstallPackage(package.Id, package.Version, false, true);
                manager.InstallPackage(package, false, true);
                var assembliesToLoad =
                    from p in getDependencies(package, manager.LocalRepository).Distinct()
                    from a in getAssemblies(p)
                    select new { name = new AssemblyName(a.Name), path = Path.Combine(manager.LocalRepository.Source ,manager.PathResolver.GetPackageDirectory(p), a.Path) };


                IEnumerable<string> packageAssemblies = getAssemblies(package)
                    .Select(a => Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(package), a.Path));
                string appConfig = package.GetFiles().Where(f => f.Path.ToLower() == @"config\app.config").Select(a=>Path.Combine(manager.LocalRepository.Source ,manager.PathResolver.GetPackageDirectory(package), a.Path)).FirstOrDefault();

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
                
                        ){ConfigFile = Path.GetFullPath(appConfig)}
                    );
            }


            return res;

            var first = manager.LocalRepository.GetPackages().First(p => p.Id == packages.First().Id);
            var ids = first.DependencySets.SelectMany(s => s.Dependencies.Select(d => d.Id)).ToArray();


//            manager.InstallPackage("NugetTestApp", new SemanticVersion(1,0,0,0), false, true);
           return manager.LocalRepository.GetPackages()
                .Select(p=>
                    new HostedAppInfo(
                        p.Id,
                        string.Join(", ",p.Authors),
                        p.Version.Version,
                        null,
                        p.AssemblyReferences.ToDictionary(a=>new AssemblyName(a.Name),a=>a.Path),
                        null)
                        );
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


                /*var name = attribute.ConstructorArguments.First().Value.ToString();
                var vendor = attribute.ConstructorArguments.Count == 2 ? attribute.ConstructorArguments[1].Value.ToString() : HostedApplicationAttribute.DEFAULT_VENDOR;
*/
                return appType.FullName + ", " + assembly.FullName;
            }
        
            throw new ConfigurationErrorsException("Application class not found");
        }
    }


    class RepositoryWrapper : IPackageRepository
    {
        private readonly IPackageRepository m_Repository;

        public RepositoryWrapper(IPackageRepository repository)
        {
            m_Repository = repository;
        }

        public IQueryable<IPackage> GetPackages()
        {
/*
            var retur= (from file in FileSystem.GetFiles("", "*" + Constants.PackageExtension)
                    let packageName = Path.GetFileNameWithoutExtension(file)
                    where FileSystem.DirectoryExists(packageName)
                    select new UnzippedPackage(FileSystem, packageName)).AsQueryable();
*/


            return m_Repository.GetPackages().Select(package => new PackageWrapper(package));
           // return packageWrappers.AsQueryable<IPackage>();
/*
            return (from package in m_Repository.GetPackages()
                    select new PackageWrapper(package) as IPackage).AsQueryable();
*/

            //return m_Repository.GetPackages().Select<IPackage,IPackage>(package => new PackageWrapper(package));
        }

        public void AddPackage(IPackage package)
        {
            m_Repository.AddPackage(package);
        }

        public void RemovePackage(IPackage package)
        {
            m_Repository.RemovePackage(package);
        }

        public string Source
        {
            get { return m_Repository.Source; }
        }

        public bool SupportsPrereleasePackages
        {
            get { return m_Repository.SupportsPrereleasePackages; }
        }
    }

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