using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Castle.Facilities.Logging;
using Castle.Windsor.Diagnostics.Extensions;
using Inceptum.AppServer.AppDiscovery.Nuget;
using Inceptum.AppServer.Model;
using NUnit.Framework;
using Castle.Core.Logging;
using Castle.Facilities.Startable;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.Resolvers.SpecializedResolvers;
using Castle.Windsor;
using NuGet;
using ILogger = Castle.Core.Logging.ILogger;

namespace Inceptum.AppServer.Tests
{
    public class MyComponent:IDisposable
    {
        public bool IsDisposed { get; private set; }

        public MyComponent(ILogger logger,string name)
        {
        }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }
    
   public  interface IMyComponentFactory
    {
        MyComponent Create(string name);
        void Release(MyComponent component);
    }

    class Manager:IDisposable
    {
        private readonly IMyComponentFactory m_Factory;
        private readonly List<MyComponent> m_Components=new List<MyComponent>();

        public Manager(IMyComponentFactory factory)
        {
            m_Factory = factory;
        }

        public void InitializeComponent(string name)
        {
            m_Components.Add(m_Factory.Create(name));
        }

        public void Dispose()
        {
            foreach (var component in m_Components)
            {
                m_Factory.Release(component);
            }
        }
    }

    [TestFixture]
    public class Investigation
    {
        [Test]
        [Ignore]
        public void Test()
        {
            using (var container = new WindsorContainer())
            {
                container
                    .AddFacility<TypedFactoryFacility>()
                    .AddFacility<LoggingFacility>()
                    .Register(
                        Component.For<IMyComponentFactory>().AsFactory(),
                        Component.For<MyComponent>().LifestyleTransient(),
                        Component.For<Manager>()
                    );

                var manager = container.Resolve<Manager>();

                manager.InitializeComponent("component1");
                manager.InitializeComponent("component2");
            }
        }

    }
    [TestFixture]
    public class NugetInvestigation
    {
        [Test]
        [Ignore]
        public void ProgetResolutionErrorDemoTest()
        {
            var tmp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            string targetRepo=Path.Combine(tmp,"TargetRepo");
            string sourceRepo = Path.Combine(tmp, "SourceRepo");

            Directory.CreateDirectory(sourceRepo);
            Directory.CreateDirectory(targetRepo);
            Console.WriteLine(sourceRepo);



            //Create empty package with dependency on RavenDB.Client 2.5.2700
            var metadata = new ManifestMetadata()
            {
                Authors = "none",
                Version = "1.0.0.0",
                Id = "RavenDependentPackage",
                Description = "A description",
            };

            var builder = new PackageBuilder();
            builder.DependencySets.Add(new PackageDependencySet(new FrameworkName(".NETFramework,Version=v4.5"), new[] { new PackageDependency("RavenDB.Client", new VersionSpec(new SemanticVersion("2.5.2700"))) }));
            builder.Populate(metadata);
            using (FileStream stream = File.Open(Path.Combine(sourceRepo, "RavenDependentPackage.1.0.0.0.nupkg"), FileMode.OpenOrCreate))
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
                var manager = new PackageManager(aggregateRepository, targetRepo) { Logger = new NugetConsoleLogger() };
                
                //just created package lookup in temporary folder repostory
                var package = localRepo.GetPackages().Where(p => p.Id == "RavenDependentPackage" && p.IsLatestVersion).First();

                //install from manager aware of proget and temporary folder repostories
                manager.InstallPackage(package, false, false);
            }
            finally
            {
                 Directory.Delete(targetRepo,true);
            }
          
        }

        public class NugetConsoleLogger : NuGet.ILogger
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

    }

 }