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
        static readonly ManualResetEvent m_InstanceStopped = new ManualResetEvent(false);
        private static ApplicationInitializer m_ApplicationInitializer;
        private static ServiceHost m_ServiceHost;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SetWindowText(IntPtr hWnd, string windowName);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleTitle(string text);

        public static void Main(params string[] args)
        {
            System.Diagnostics.Debugger.Launch();
            var name = args[0];
            AppDomain.CurrentDomain.SetData("AppServer.Application",name);
            m_ApplicationInitializer = new ApplicationInitializer();

            IntPtr handle = Process.GetCurrentProcess().MainWindowHandle;

            SetConsoleTitle("AppServer - " + name); 
            SetWindowText(handle, "AppServer - " + name);
 
            var uri = "net.pipe://localhost/AppServer/" + WndUtils.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/instances/" + name;
            var factory = new ChannelFactory<IApplicationInstance>(new NetNamedPipeBinding(), new EndpointAddress(uri));
            IApplicationInstance instance = factory.CreateChannel();
            var instanceParams = instance.GetInstanceParams();
            

            m_ApplicationInitializer.Initialize(instanceParams.ApplicationParams.AssembliesToLoad, instanceParams.ApplicationParams.NativeDllToLoad, null);
            var hostType = typeof(ApplicationHost<>).MakeGenericType(Type.GetType(instanceParams.ApplicationParams.AppType));




            var appHost = Activator.CreateInstance(hostType, getLogCache(), getCongigurationProvider(), instanceParams.Environment, name, instanceParams.AppServerContext);

            m_ServiceHost = new ServiceHost(appHost);
            var address = new Uri("net.pipe://localhost/AppServer/" + Process.GetCurrentProcess().Id + "/" + name);
            //TODO: need to do it in better way. String based type resolving is a bug source
            m_ServiceHost.AddServiceEndpoint(typeof(IApplicationHost), new NetNamedPipeBinding(), address);
            //m_ConfigurationProviderServiceHost.Faulted += new EventHandler(this.IpcHost_Faulted);
            m_ServiceHost.Open();
            

            instance.RegisterApplicationHost(address.ToString());
            factory.Close();
            m_InstanceStopped.WaitOne();

        }


        private static IConfigurationProvider getCongigurationProvider()
        {
            var uri = "net.pipe://localhost/AppServer/" + WndUtils.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/ConfigurationProvider";
            var factory = new ChannelFactory<IConfigurationProvider>(new NetNamedPipeBinding(),
                new EndpointAddress(uri));
            return factory.CreateChannel();
        }

        private static ILogCache getLogCache()
        {
            var uri = "net.pipe://localhost/AppServer/" + WndUtils.GetParentProcess(Process.GetCurrentProcess().Handle).Id + "/LogCache";
            var factory = new ChannelFactory<ILogCache>(new NetNamedPipeBinding(),
                new EndpointAddress(uri));
            return factory.CreateChannel();
        }
    }


  
}