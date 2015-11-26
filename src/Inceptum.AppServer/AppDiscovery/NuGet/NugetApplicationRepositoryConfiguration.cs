using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal class NugetApplicationRepositoryConfiguration
    {
        public bool? AllowPrereleaseVersions { get; set; }
        public DependencyVersion? DependencyVersion { get; set; }
        public string ApplicationRepository { get; set; }
        public string[] DependenciesRepositories { get; set; }
    }
}