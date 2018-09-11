using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;
using NUnit.Framework;

namespace Inceptum.AppServer.Tests
{
    [TestFixture]
    public class NugetInvestigation
    {
        public class NugetConsoleLogger : global::NuGet.ILogger
        {
            public FileConflictResolution ResolveFileConflict(string message)
            {
                return FileConflictResolution.Ignore;
            }

            public void Log(MessageLevel level, string message, params object[] args)
            {
                Console.WriteLine("[" + level + "] " + message, args);
            }
        }

        [Test]
        [Ignore]
        public void ProgetResolutionErrorDemoTest()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            var targetRepo = Path.Combine(tmp, "TargetRepo");
            var sourceRepo = Path.Combine(tmp, "SourceRepo");

            Directory.CreateDirectory(sourceRepo);
            Directory.CreateDirectory(targetRepo);
            Console.WriteLine(sourceRepo);


            //Create empty package with dependency on RavenDB.Client 2.5.2700
            var metadata = new ManifestMetadata
            {
                Authors = "none",
                Version = "1.0.0.0",
                Id = "RavenDependentPackage",
                Description = "A description"
            };

            var builder = new PackageBuilder();
            builder.DependencySets.Add(new PackageDependencySet(new FrameworkName(".NETFramework,Version=v4.5"),
                new[] {new PackageDependency("RavenDB.Client", new VersionSpec(new SemanticVersion("2.5.2700")))}));
            builder.Populate(metadata);
            using (var stream = File.Open(Path.Combine(sourceRepo, "RavenDependentPackage.1.0.0.0.nupkg"), FileMode.OpenOrCreate))
            {
                builder.Save(stream);
            }


            try
            {
                //Repository hosted by [proget with connector to nuget.org
                var progetRepo = PackageRepositoryFactory.Default.CreateRepository("http://nuget.it.unistreambank.ru/nuget/DEV.Libs");
                //Temporary folder weher package would be installed
                var localRepo = PackageRepositoryFactory.Default.CreateRepository(sourceRepo);

                var aggregateRepository = new AggregateRepository(new[] {progetRepo, localRepo});
                var manager = new PackageManager(aggregateRepository, targetRepo) {Logger = new NugetConsoleLogger()};

                //just created package lookup in temporary folder repostory
                var package = localRepo.GetPackages().Where(p => p.Id == "RavenDependentPackage" && p.IsLatestVersion).First();

                //install from manager aware of proget and temporary folder repostories
                manager.InstallPackage(package, false, false);
            }
            finally
            {
                Directory.Delete(targetRepo, true);
            }
        }
    }
}