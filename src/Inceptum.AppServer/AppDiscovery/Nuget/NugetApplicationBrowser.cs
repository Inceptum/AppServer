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

             string repoPath =Path.GetFullPath(@"..\..\..\..\TestData\NugetRepo") ;
            IPackageRepository appsRepo = PackageRepositoryFactory.Default.CreateRepository(repoPath);
            IPackageRepository dependenciesRepo = PackageRepositoryFactory.Default.CreateRepository("https://nuget.org/api/v2/");
            IPackageRepository repository =
                new AggregateRepository(new[]
                    {
                        appsRepo,
                        dependenciesRepo
                    });
            var appsRepoManager = new PackageManager(appsRepo,  @".\NugetLoaclRepo");
            var manager = new PackageManager(repository,  @".\NugetLoaclRepo");

            IPackage[] packages = appsRepoManager.SourceRepository.GetPackages().OrderBy(p => p.Id).ToArray();


            List<HostedAppInfo> res = new List<HostedAppInfo>();

            foreach (var package in packages)
            {
                manager.InstallPackage(package.Id, package.Version, false, true);
                var assembliesToLoad =
                    from p in getDependencies(package, manager.LocalRepository).Distinct()
                    from a in getAssemblies(p)
                    select new { name = new AssemblyName(a.Name), path = Path.Combine(manager.LocalRepository.Source ,manager.PathResolver.GetPackageDirectory(p), a.Path) };


                IEnumerable<string> packageAssemblies = getAssemblies(package)
                    .Select(a => Path.Combine(manager.LocalRepository.Source, manager.PathResolver.GetPackageDirectory(package), a.Path));

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
                
                        )
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
}