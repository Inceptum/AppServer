using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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

        public NugetApplicationBrowser(ILogger logger, string applicationRepository, params string[] dependenciesRepositories)
        {
            Logger = logger;
            m_DependenciesRepositories = dependenciesRepositories.Select(getRepositoryPath).ToArray();
            m_ApplicationRepository = getRepositoryPath(applicationRepository);
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

        private IEnumerable<IPackageFile> getAssemblies(IPackage package)
        {
            IEnumerable<IPackageFile> refs;
            if (!VersionUtility.TryGetCompatibleItems(NET40, package.GetLibFiles().Where(f => f.EffectivePath.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase)), out refs))
            {
                throw new ConfigurationErrorsException(string.Format("Failed to load package {0} since it is not compartible with .Net 4.0",package.Id));
            }
 /*
            if (!VersionUtility.TryGetCompatibleItems(NET40, package.AssemblyReferences, out refs))
            {
                throw new ConfigurationErrorsException(string.Format("Failed to load package {0} since it is not compartible with .Net 4.0",package.Id));
            }
            IEnumerable<IPackageFile> satellites;
            VersionUtility.TryGetCompatibleItems(NET40, package.GetLibFiles().Where(f=>f.EffectivePath.EndsWith(".resources.dll",StringComparison.InvariantCultureIgnoreCase)), out satellites);
            var packageFiles = satellites.ToArray();
            return refs.Concat(packageFiles);
 */
            return refs.ToArray();
        }
        
        public string Name
        {
            get { return "Nuget"; }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
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
                dependencies = getDependencies(package, manager.LocalRepository).Distinct().ToArray();
                Logger.WarnFormat("Installed {0}", string.Join(",",dependencies.Select(p=>p.Id)));
            }

            var assembliesToLoad =
                from p in dependencies
                from a in getAssemblies(p)
                select
                    new
                        {
                            //name = new AssemblyName(a.Name),
                            path = Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(p), a.Path)
                        };

          

            var nativesToLoad =
                (from p in dependencies
                from a in p.GetFiles("unmanaged")
                 select Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(p), a.Path)).ToArray();

            IEnumerable<string> packageAssemblies = getAssemblies(package)
                .Select(a => Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(package), a.Path));
            string[] appConfigs = package.GetFiles().Where(f => f.Path.ToLower().StartsWith(@"config\"))
                                      .Select(c => Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(package), c.Path))
                                      .Select(Path.GetFullPath).ToArray();

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
/*
                    if(a.path.EndsWith(".resources.dll",StringComparison.InvariantCultureIgnoreCase))
                        assemblies.Add(new AssemblyName(assembly.FullName), a.path);
                    else*/
                    var assemblyName = new AssemblyName(assembly.Name.Name);
                    if (a.path.EndsWith(".resources.dll", StringComparison.InvariantCultureIgnoreCase))
                        assemblyName.CultureInfo = new CultureInfo(assembly.Name.Culture);
                    assemblies.Add(assemblyName, a.path);
                }
            }

            assemblies = assemblies.GroupBy(a => a.Key.FullName).ToDictionary(g =>g.First().Key,g=>g.First().Value );
            return new ApplicationParams(getAppType(packageAssemblies), appConfigs, nativesToLoad, assemblies);
        }

        public IEnumerable<HostedAppInfo> GetAvailableApps()
        {
            IPackageRepository appsRepo = PackageRepositoryFactory.Default.CreateRepository(m_ApplicationRepository);
            var packages = from p in appsRepo.GetPackages() where p.Tags != null && p.Tags.Contains("Inceptum.AppServer.Application") orderby p.Id select p;
            return packages.ToArray().Select(package => new HostedAppInfo(
                                                  package.Id,
                                                  string.Join(", ", package.Authors),
                                                  package.Version.Version
                                                  )).ToArray();
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
}