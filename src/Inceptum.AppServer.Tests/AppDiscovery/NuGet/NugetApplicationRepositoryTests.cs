using System;
using System.IO;
using System.Reflection;
using Castle.Core.Logging;
using Inceptum.AppServer.AppDiscovery;
using Inceptum.AppServer.AppDiscovery.NuGet;
using NuGet;
using NUnit.Framework;

namespace Inceptum.AppServer.Tests.AppDiscovery.NuGet
{
    [TestFixture]
    internal class NugetApplicationRepositoryTests
    {
        [Test]
        [ExpectedException(typeof(NuGetVersionNotSatisfiedException))]
        public void ShouldFailOnUnsupportedDependencyPackageBetaVersion()
        {
            var applicationInfo = new ApplicationInfo
            {
                ApplicationId = "Unistream.Processing.Integration",
                Debug = false,
                Description = "Processing operations",
                Vendor = "Unistream",
                Version = Version.Parse("1.0.8.25")
            };
            var repository = createRepository(true, DependencyVersion.Highest);

            executeInTempDirectory(testAppPath => repository.Install(testAppPath, applicationInfo));
        }

        [Test]
        public void ShouldInstallHighestDependencyPackageVersion()
        {
            var applicationInfo = new ApplicationInfo
            {
                ApplicationId = "Unistream.Processing.Integration",
                Debug = false,
                Description = "Processing operations",
                Vendor = "Unistream",
                Version = Version.Parse("1.0.8.25")
            };
            var repository = createRepository(false, DependencyVersion.Highest);

            executeInTempDirectory(testAppPath =>
            {
                repository.Install(testAppPath, applicationInfo);
                var assemblyFileName = Path.Combine(testAppPath, "bin", "protobuf-net.dll");
                var assembly = Assembly.Load(File.ReadAllBytes(assemblyFileName));
                Assert.AreEqual("protobuf-net, Version=2.0.0.668, Culture=neutral, PublicKeyToken=257b51d87d2e4d67", assembly.ToString());
            });
        }

        [Test]
        public void ShouldInstallLowestDependencyPackageVersion()
        {
            var applicationInfo = new ApplicationInfo
            {
                ApplicationId = "Unistream.Processing.Integration",
                Debug = false,
                Description = "Processing operations",
                Vendor = "Unistream",
                Version = Version.Parse("1.0.8.25")
            };
            var repository = createRepository(false, DependencyVersion.Lowest);

            executeInTempDirectory(testAppPath =>
            {
                repository.Install(testAppPath, applicationInfo);
                var assemblyFileName = Path.Combine(testAppPath, "bin", "protobuf-net.dll");
                var assembly = Assembly.Load(File.ReadAllBytes(assemblyFileName));
                Assert.AreEqual("protobuf-net, Version=2.0.0.640, Culture=neutral, PublicKeyToken=257b51d87d2e4d67", assembly.ToString());
            });
        }

        static void executeInTempDirectory(Action<string> action)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), DateTime.Now.Ticks.ToString());

            try
            {
                Directory.CreateDirectory(path);

                action(path);
            }
            finally
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
        }

        static NugetApplicationRepository createRepository(bool allowPrereleaseVersions, DependencyVersion dependencyVersion)
        {
            var logger = new ConsoleLogger();
            var applicationsRepositorySource = @"E:\Temp\nuget";//"http://nuget.it.unistreambank.ru/nuget/DEV.Apps";
            var dependencyRepositoriesSet = new[]
            {
                "http://nuget.it.unistreambank.ru/nuget/DEV.Libs",
                "http://nuget.it.unistreambank.ru/nuget/DEV.ThirdParty"
            };

            var repository = new NugetApplicationRepository(logger, applicationsRepositorySource, dependencyRepositoriesSet, allowPrereleaseVersions, dependencyVersion);

            return repository;
        }
    }
}