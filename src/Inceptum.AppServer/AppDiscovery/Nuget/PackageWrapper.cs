using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.Nuget
{
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


        public Version MinClientVersion
        {
            get { return m_Package.MinClientVersion; }
        }

        public ICollection<PackageReferenceSet> PackageAssemblyReferences
        {
            get { return m_Package.PackageAssemblyReferences; }
        }
    }

  
}