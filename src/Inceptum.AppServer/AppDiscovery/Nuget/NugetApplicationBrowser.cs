using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using Inceptum.AppServer.Model;
using Mono.Cecil;
using NuGet;
using Castle.Core.Logging;
using ILogger = Castle.Core.Logging.ILogger;

namespace Inceptum.AppServer.AppDiscovery.Nuget
{
    public class NugetApplicationBrowser : IApplicationBrowser, NuGet.ILogger
    {
        private ILogger Logger { get; set; }
        internal static readonly FrameworkName NET40= new FrameworkName(".NETFramework,Version=v4.5");
        private readonly string m_ApplicationRepository;
        private readonly string[] m_DependenciesRepositories;

        public NugetApplicationBrowser(ILogger logger,string applicationRepository, params string[] dependenciesRepositories)
        {
            applicationRepository=Path.Combine(AppDomain.CurrentDomain.BaseDirectory, applicationRepository);
            Logger = logger;
            m_DependenciesRepositories = dependenciesRepositories.Select(
                r => Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, r)) ? Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, r)) : r).ToArray();
            m_ApplicationRepository = Directory.Exists(applicationRepository) ? Path.GetFullPath(applicationRepository) : applicationRepository;
        }

        private IEnumerable<IPackage> getDependencies(IPackage package, IPackageRepository repository)
        {
            if (package == null)
                return new IPackage[] {null};
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


        public ApplicationParams GetApplicationParams(string application, Version version)
        {
            IPackageRepository appsRepo = PackageRepositoryFactory.Default.CreateRepository(m_ApplicationRepository);
            IPackageRepository[] dependencyRepositories = m_DependenciesRepositories.Select(r => PackageRepositoryFactory.Default.CreateRepository(r)).ToArray();
            var dependencyRepo = new AggregateRepository(
                new[] {appsRepo}.Concat(dependencyRepositories)
                );

            var manager = new PackageManager(dependencyRepo, Path.Combine(AppDomain.CurrentDomain.BaseDirectory,@".\NugetLocalRepo")) {Logger = this};
            
            IPackage[] dependencies=null;
            PackageWrapper package=null;

            var foundPackage = manager.LocalRepository.FindPackage(application, new SemanticVersion(version));
            if (foundPackage != null)
            {
                package = new PackageWrapper(foundPackage, dependencyRepo);
                dependencies = getDependencies(package, manager.LocalRepository).Distinct().ToArray();
            }
            if(package==null || dependencies.Any(d=>d==null))
            {
                if(package==null)
                    Logger.InfoFormat("Application {0} v{1} not found. Installing", application, version);
                else
                    Logger.WarnFormat("Application {0} v{1} dependencies are corrupted. Reinstalling", application, version);
                var pkg = appsRepo.FindPackage(application, new SemanticVersion(version));
                var remotePackage = new PackageWrapper(pkg, dependencyRepo);
                manager.InstallPackage(remotePackage, false, true);
                package = new PackageWrapper(manager.LocalRepository.FindPackage(application, new SemanticVersion(version)), dependencyRepo);
                //TODO: find out why null is in array
                dependencies = getDependencies(package, manager.LocalRepository).Distinct().Where(p=>p!=null).ToArray();
                Logger.WarnFormat("Installed {0}", string.Join(",",dependencies.Select(p=>p.Id)));
            }

            var assembliesToLoad =
                from p in dependencies
                from a in getAssemblies(p)
                select
                    new
                        {
                            name = new AssemblyName(a.Name),
                            path = Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(p), a.Path)
                        };

            var nativesToLoad =
                (from p in dependencies
                from a in p.GetFiles("unmanaged")
                 select Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(p), a.Path)).ToArray();

            IEnumerable<string> packageAssemblies = getAssemblies(package)
                .Select(a => Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(package), a.Path));
            string appConfig = package.GetFiles().Where(f => f.Path.ToLower() == @"config\app.config")
                                      .Select(c => Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(package), c.Path))
                                      .Select(Path.GetFullPath).FirstOrDefault();
            Logger.Debug("Assemblies to load: "+string.Join(Environment.NewLine,assembliesToLoad.Select(a=>a.path).ToArray()));



            var assemblies=new Dictionary<AssemblyName, string>();
            foreach (var a in assembliesToLoad)
            {
                var assembly = CeceilExtencions.TryReadAssembly(a.path);

                if (assembly == null)
                {
                    //BUG: Microsoft.Bcl.1.1.3 contains file _._ which loads as assembly while it is crap 
                    Logger.WarnFormat("Failed to load assembly {0}", a.path);
                }
                else
                {
                    assemblies.Add(new AssemblyName(assembly.Name.Name),a.path);
                }
            }
            return new ApplicationParams(getAppType(packageAssemblies), appConfig, nativesToLoad, assemblies);
        }

        public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {
            IPackageRepository appsRepo = PackageRepositoryFactory.Default.CreateRepository(m_ApplicationRepository);
            var packages = from p in appsRepo.GetPackages() where p.Tags != null && p.Tags.Contains("Inceptum.AppServer.Application") orderby p.Id select p;
            return packages.ToArray().Select(package => new HostedAppInfo(
                                                  package.Id,
                                                  string.Join(", ", package.Authors),
                                                  package.Version.Version
                                                  ));
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

        void NuGet.ILogger.Log(MessageLevel level, string message, params object[] args)
        {
            switch (level)
            {
                    case MessageLevel.Debug:
                        Logger.DebugFormat(message,args);
                        break;
                    case MessageLevel.Error:
                        Logger.ErrorFormat(message, args);
                        break;
                    case MessageLevel.Info:
                        Logger.InfoFormat(message, args);
                        break;
                    case MessageLevel.Warning:
                        Logger.WarnFormat(message,args);
                        break;
            }
        }
    }


    /// <summary>
    /// Filters out Inceptum.AppServer.Sdk dependency
    /// </summary>
    class PackageWrapper:IPackage
    {
        private readonly IPackage m_Package;
        private readonly IEnumerable<PackageDependencySet> m_PackageDependencySets;

        public PackageWrapper(IPackage package, IPackageRepository sdkRepo)
        {
            m_Package = package;
            PackageDependency sdkDependency = package.FindDependency("Inceptum.AppServer.Sdk", NugetApplicationBrowser.NET40);
            if (sdkDependency == null)
            {
                m_PackageDependencySets = m_Package.DependencySets;
                return;
            }
            IPackage sdkPackage = sdkRepo.FindPackage(sdkDependency.Id, sdkDependency.VersionSpec, true, false);
            if(sdkPackage==null)
                throw new InvalidOperationException(string.Format("Package {0} references unresolvable SDk version {1}",package,sdkDependency));
            SdkVersion = sdkDependency.VersionSpec;
            m_PackageDependencySets = m_Package.DependencySets.Select(ds => new PackageDependencySet(
                                                                                ds.TargetFramework,
                                                                                ds.Dependencies
                                                                                  .Where(dependency => dependency.Id != "Inceptum.AppServer.Sdk")
                                                                                  .Concat(sdkPackage.DependencySets
                                                                                                    .Where(sds => sds.TargetFramework == ds.TargetFramework)
                                                                                                    .SelectMany(sds => sds.Dependencies.Where(dep => ds.Dependencies.All(d => d.Id != dep.Id))))
                                                                                )).Where(ds => ds.Dependencies.Any()).ToArray();
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
            get;private set;
        }

        public IEnumerable<PackageDependencySet> DependencySets
        {
            get
            {
                return m_PackageDependencySets;
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