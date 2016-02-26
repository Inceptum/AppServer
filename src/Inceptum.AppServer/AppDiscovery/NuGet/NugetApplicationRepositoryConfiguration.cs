using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal class NugetApplicationRepositoryConfiguration
    {
        public NugetApplicationRepositoryConfiguration()
        {
            DependencyVersion=global::NuGet.DependencyVersion.Lowest;
        }
        public bool? AllowPrereleaseVersions { get; set; }
        public DependencyVersion? DependencyVersion { get; set; }
        public string ApplicationRepository { get; set; }
        public string[] DependenciesRepositories { get; set; }
    }
}