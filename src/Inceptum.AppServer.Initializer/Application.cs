using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Hosting;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Model;

namespace Inceptum.AppServer.Initializer
{

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    class ApplicationInitializer:IApplicationInitializer
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        private Dictionary<AssemblyName, Lazy<Assembly>> m_LoadedAssemblies;
         
        public ApplicationInitializer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
        }

        public void Initialize(Dictionary<string, string> assembliesToLoad, IEnumerable<string> nativeDllToLoad, string applicationHostType/*, string applicationType*/)
        {
            /*AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
            {
                crashHandler.Handle(args.ExceptionObject.ToString());
            };*/
            IEnumerable<AssemblyName> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName());

            m_LoadedAssemblies = assembliesToLoad.Where(asm => loadedAssemblies.All(a => a.Name != new AssemblyName(asm.Key).Name))
                                                 .ToDictionary(asm =>new AssemblyName(asm.Key), asm => new Lazy<Assembly>(() => loadAssembly(asm.Value)));

            foreach (string dll in nativeDllToLoad)
            {
                if (LoadLibrary(dll) == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to load unmanaged dll " + Path.GetFileName(dll) + " from package");
                }
            }


        }

        private static Assembly loadAssembly(string path)
        {
            byte[] assemblyBytes = File.ReadAllBytes(path);
            var pdb = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".pdb");
            if (File.Exists(pdb))
            {
                byte[] pdbBytes = File.ReadAllBytes(pdb);
                return Assembly.Load(assemblyBytes, pdbBytes);
            }

            return Assembly.Load(assemblyBytes);
        }

        private Assembly onAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly =
                loadedAssemblies.FirstOrDefault(
                    a => a.FullName == args.Name || a.GetName().Name == new AssemblyName(args.Name).Name || a.GetName().Name == args.Name
                    );

            if (assembly != null)
                return assembly;

            assembly = (from asm in m_LoadedAssemblies
                        where asm.Key.Name == new AssemblyName(args.Name).Name
                        orderby asm.Key.Version descending
                        select asm.Value.Value).FirstOrDefault();


            return assembly;
        }

    }


  
    [ServiceContract]
    public interface IApplicationInitializer  
    { 
        [OperationContract]
        void Initialize(Dictionary<string, string> assembliesToLoad, IEnumerable<string> nativeDllToLoad, string applicationHostType/*, string applicationType*/);
    }

    internal static class Application
    {
        static ManualResetEvent m_InstanceStopped = new ManualResetEvent(false);
        private static ApplicationInitializer m_ApplicationInitializer;
        private static ServiceHost m_ServiceHost;

        public static void Main(params string[] args)
        {
            System.Diagnostics.Debugger.Launch();
            var name = args[0];
            Name = name;
            AppDomain.CurrentDomain.SetData("AppServer.Application",name);
            m_ApplicationInitializer = new ApplicationInitializer();
            
/*
            var uri = "net.pipe://localhost/AppServer/" + PROCESS_BASIC_INFORMATION.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/ConfigurationProvider";
            var factory=new ChannelFactory<IConfigurationProvider>(new NetNamedPipeBinding(),
                new EndpointAddress(uri));
            IConfigurationProvider provider = factory.CreateChannel();
            var bundle=provider.GetBundle("AppServer", "server.host");
*/
       
            
         /*    var uri = "net.pipe://localhost/AppServer/" + PROCESS_BASIC_INFORMATION.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/instances/" + name+"Params";
             var nonDuplexFactory = new ChannelFactory<IApplicationInstance>(new NetNamedPipeBinding(),new EndpointAddress(uri));
             IApplicationInstance instance = nonDuplexFactory.CreateChannel();
             var test=instance.Test();*/


/*
             var factory = new DuplexChannelFactory<IApplicationInstance>(new ApplicationInitializer(), new NetNamedPipeBinding(),
                   new EndpointAddress("net.pipe://localhost/AppServer/" + PROCESS_BASIC_INFORMATION.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/instances/"+name));
*/
            getCongigurationProvider();


            var uri = "net.pipe://localhost/AppServer/" + PROCESS_BASIC_INFORMATION.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/instances/" + name;
            var factory = new ChannelFactory<IApplicationInstance>(new NetNamedPipeBinding(), new EndpointAddress(uri));
            IApplicationInstance instance = factory.CreateChannel();
            var instanceParams = instance.GetInstanceParams();

            m_ApplicationInitializer.Initialize(instanceParams.ApplicationParams.AssembliesToLoad, instanceParams.ApplicationParams.NativeDllToLoad, null);
            var hostType = Type.GetType(instanceParams.AppHostType).MakeGenericType(Type.GetType(instanceParams.ApplicationParams.AppType));




            var appHost = Activator.CreateInstance(hostType, getLogCache(), getCongigurationProvider(), instanceParams.Environment, name, instanceParams.AppServerContext);
            Console.WriteLine("!!!");

            m_ServiceHost = new ServiceHost(appHost);
            var address = new Uri("net.pipe://localhost/AppServer/" + Process.GetCurrentProcess().Id + "/" + name);
            //TODO: need to do it in better way. String based type resolving is a bug source
            m_ServiceHost.AddServiceEndpoint(hostType.GetInterface("IApplicationHost2"), new NetNamedPipeBinding(), address);
            //m_ConfigurationProviderServiceHost.Faulted += new EventHandler(this.IpcHost_Faulted);
            m_ServiceHost.Open();


            instance.RegisterApplicationHost(address.ToString());
            m_InstanceStopped.WaitOne();

        }

        public static string Name { get; set; }

        private static IConfigurationProvider getCongigurationProvider()
        {
            var uri = "net.pipe://localhost/AppServer/" + PROCESS_BASIC_INFORMATION.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/ConfigurationProvider";
            var factory = new ChannelFactory<IConfigurationProvider>(new NetNamedPipeBinding(),
                new EndpointAddress(uri));
            return factory.CreateChannel();
        }
        private static ILogCache getLogCache()
        {
            var uri = "net.pipe://localhost/AppServer/" + PROCESS_BASIC_INFORMATION.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/LogCache";
            var factory = new ChannelFactory<ILogCache>(new NetNamedPipeBinding(),
                new EndpointAddress(uri));
            return factory.CreateChannel();
        }
    }


    /// <summary>
    /// The proces s_ basi c_ information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct PROCESS_BASIC_INFORMATION
    {
        // These members must match PROCESS_BASIC_INFORMATION
        /// <summary>
        ///   The reserved 1.
        /// </summary>
        internal IntPtr Reserved1;

        /// <summary>
        ///   The peb base address.
        /// </summary>
        internal IntPtr PebBaseAddress;

        /// <summary>
        ///   The reserved 2_0.
        /// </summary>
        internal IntPtr Reserved2_0;

        /// <summary>
        ///   The reserved 2_1.
        /// </summary>
        internal IntPtr Reserved2_1;

        /// <summary>
        ///   The unique process id.
        /// </summary>
        internal IntPtr UniqueProcessId;

        /// <summary>
        ///   The inherited from unique process id.
        /// </summary>
        internal IntPtr InheritedFromUniqueProcessId;

        /// <summary>
        /// The nt query information process.
        /// </summary>
        /// <param name="processHandle">
        /// The process handle. 
        /// </param>
        /// <param name="processInformationClass">
        /// The process information class. 
        /// </param>
        /// <param name="processInformation">
        /// The process information. 
        /// </param>
        /// <param name="processInformationLength">
        /// The process information length. 
        /// </param>
        /// <param name="returnLength">
        /// The return length. 
        /// </param>
        /// <returns>
        /// The nt query information process. 
        /// </returns>
        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref PROCESS_BASIC_INFORMATION processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">
        /// The process handle. 
        /// </param>
        /// <returns>
        /// An instance of the Process class. 
        /// </returns>
        public static Process GetParentProcess(IntPtr handle)
        {
            PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
            {
                throw new Win32Exception(status);
            }

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }
}