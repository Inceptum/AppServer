using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Versioning;
using System.Threading;
using Castle.Facilities.Logging;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NuGet;
using NUnit.Framework;
using ILogger = Castle.Core.Logging.ILogger;

namespace Inceptum.AppServer.Tests
{
    public class MyComponent : IDisposable
    {
        public MyComponent(ILogger logger, string name)
        {
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            IsDisposed = true;
        }
    }

    public interface IMyComponentFactory
    {
        MyComponent Create(string name);
        void Release(MyComponent component);
    }

    internal class Manager : IDisposable
    {
        private readonly List<MyComponent> m_Components = new List<MyComponent>();
        private readonly IMyComponentFactory m_Factory;

        public Manager(IMyComponentFactory factory)
        {
            m_Factory = factory;
        }

        public void Dispose()
        {
            foreach (var component in m_Components)
            {
                m_Factory.Release(component);
            }
        }

        public void InitializeComponent(string name)
        {
            m_Components.Add(m_Factory.Create(name));
        }
    }

    [TestFixture]
    public class Investigation
    {
        public static Process GenerateRuntimeProcess(string processName, int aliveDuration, bool throwOnException = true)
        {
            Process result = null;
            try
            {
                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(new AssemblyName {Name = processName}, AssemblyBuilderAccess.RunAndSave);
                //assemblyBuilder.GetReferencedAssemblies()


                var constructorInfo = typeof (AssemblyDescriptionAttribute).GetConstructor(new[] {typeof (string)});
                var attributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] {"Test Assembly - AssemblyDescriptionAttribute"});
                assemblyBuilder.SetCustomAttribute(attributeBuilder);


                constructorInfo = typeof (AssemblyTitleAttribute).GetConstructor(new[] {typeof (string)});
                attributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] {"Test Assembly - AssemblyTitleAttribute"});
                assemblyBuilder.SetCustomAttribute(attributeBuilder);

                constructorInfo = typeof (AssemblyProductAttribute).GetConstructor(new[] {typeof (string)});
                attributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] {"Test Assembly - AssemblyDescriptionAttribute"});
                assemblyBuilder.SetCustomAttribute(attributeBuilder);

                constructorInfo = typeof (AssemblyTrademarkAttribute).GetConstructor(new[] {typeof (string)});
                attributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] {"Test Assembly - AssemblyDescriptionAttribute"});
                assemblyBuilder.SetCustomAttribute(attributeBuilder);

                assemblyBuilder.DefineVersionInfoResource();


                var moduleBuilder = assemblyBuilder.DefineDynamicModule(processName, processName + ".EXE");
                var typeBuilder = moduleBuilder.DefineType("Program", TypeAttributes.Public);
                var methodBuilder = typeBuilder.DefineMethod("Main", MethodAttributes.Public | MethodAttributes.Static, null, null);
                var il = methodBuilder.GetILGenerator();
                il.UsingNamespace("System.Threading");
                il.EmitWriteLine("Hello World");


                var type = Assembly.LoadFrom(@"d:\CODE\inceptum_appserver\src\Inceptum.AppServer.Tests\bin\Debug\AppHost.exe").GetType("Inceptum.AppServer.AppHost.Program");
                var methodInfo = type.GetMethod("Main");
                var arr = il.DeclareLocal(typeof (string));

                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Newarr, typeof (string));
                il.Emit(OpCodes.Stloc, arr);
                il.Emit(OpCodes.Ldloc, arr);

                il.Emit(OpCodes.Ldc_I4, 0);
                il.Emit(OpCodes.Ldstr, "AAAA");
                il.Emit(OpCodes.Stelem_Ref);

                il.Emit(OpCodes.Ldloc, arr);

                /* il.Emit(OpCodes.Newarr, typeof (string));
                il.Emit(OpCodes.Ldloc_0);*/
                il.Emit(OpCodes.Call, methodInfo);

                //il.Emit(OpCodes.Call, typeof(MyClass).GetMethod("Test"));

                il.Emit(OpCodes.Ldc_I4, aliveDuration);
                il.Emit(OpCodes.Call, typeof (Thread).GetMethod("Sleep", new[] {typeof (int)}));
                il.Emit(OpCodes.Ret);
                typeBuilder.CreateType();
                assemblyBuilder.SetEntryPoint(methodBuilder.GetBaseDefinition(), PEFileKinds.ConsoleApplication);
                assemblyBuilder.Save(processName + ".EXE"); //, PortableExecutableKinds.Required32Bit, ImageFileMachine.I386);
                Console.WriteLine(Path.Combine(Environment.CurrentDirectory, processName + ".EXE"));
                result = Process.Start(new ProcessStartInfo(processName + ".EXE")
                {
                    WindowStyle = ProcessWindowStyle.Hidden
                });
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                if (throwOnException)
                {
                    throw;
                }
                result = null;
            }
            return result;
        }

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

        [Test]
        [Ignore]
        public void Test1()
        {
            var generateRuntimeProcess = GenerateRuntimeProcess("test", 60000);
            Thread.Sleep(1000000);
        }
    }

    [TestFixture]
    public class NugetInvestigation
    {
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