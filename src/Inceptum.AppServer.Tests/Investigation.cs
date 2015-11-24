using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Castle.Facilities.Logging;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using NUnit.Framework;

namespace Inceptum.AppServer.Tests
{
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
}