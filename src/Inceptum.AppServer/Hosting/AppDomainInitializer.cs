/*using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Inceptum.AppServer.Hosting
{
    /// <summary>
    /// Initializes app domain. Class is taken off to separate assembly to prevent assemblies via
    /// LoadFrom Context (assembly1 references assembly2, assembly2 is looked in same dir as assembly1) 
    /// Need all assembly resolution to happen within  AppDomain.AssemblyResolve event
    /// </summary>
    [Serializable]
    internal class AppDomainInitializer : MarshalByRefObject
    {
        private Dictionary<AssemblyName, Lazy<Assembly>> m_LoadedAssemblies;

        public AppDomainInitializer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
            AppDomain.CurrentDomain.AssemblyLoad += (sender, args) =>
                {
                    Console.WriteLine("Loaded:"+args.LoadedAssembly.FullName);
                };
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public void Initialize(string workingDirectory, Dictionary<AssemblyName, string> assembliesToLoad, string[] nativeDllToLoad)
        {
            IEnumerable<AssemblyName> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName());

            m_LoadedAssemblies = assembliesToLoad.Where(asm => loadedAssemblies.All(a => a.Name != asm.Key.Name))
                                                 .ToDictionary(asm => asm.Key, asm => new Lazy<Assembly>(() => Assembly.LoadFrom(asm.Value)));

            //Preload assemblies. Otherwise asemblies from appserver folder are loaded (while they may have wrong version)
/*            foreach (var loadedAssembly in m_LoadedAssemblies)
            {
                Assembly assembly = loadedAssembly.Value.Value;
            }#1#
            foreach (string dll in nativeDllToLoad)
            {
                if (LoadLibrary(dll) == IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to load unmanaged dll " + Path.GetFileName(dll) + " from package");
                }
            }


            //TODO: current directory is defined per process. Need to arrange app dirs somehow
            // Directory.SetCurrentDirectory(workingDirectory);
        }

 
        public IApplicationHost CreateHost(string appTypeName)
        {
            Type hostType = typeof(ApplicationHost<>).MakeGenericType(Type.GetType(appTypeName));
            return (IApplicationHost)Activator.CreateInstance(hostType);

        }

        private Assembly onAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly[] loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            Assembly assembly =
                loadedAssemblies.FirstOrDefault(
                    //a => a.GetName().Name == new AssemblyName(args.Name).Name || a.FullName == args.Name || a.GetName().Name == args.Name
                    a => (a.GetName().Name == new AssemblyName(args.Name).Name && new AssemblyName(args.Name).Name=="Inceptum.AppServer.Sdk") || a.FullName == args.Name 
                    );

            if (assembly != null)
                return assembly;

            assembly = (from asm in m_LoadedAssemblies
                        where asm.Key.Name == new AssemblyName(args.Name).Name
                        orderby asm.Key.Version descending
                        select asm.Value.Value).FirstOrDefault();


            return assembly;
        }


        /// <summary>
        ///     Obtains a lifetime service object to control the lifetime policy for this instance.
        /// </summary>
        /// <returns>
        ///     An object of type <see cref="T:System.Runtime.Remoting.Lifetime.ILease" /> used to control the lifetime policy for this instance. This is the current lifetime service object for this instance if one exists; otherwise, a new lifetime service object initialized to the value of the
        ///     <see
        ///         cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime" />
        ///     property.
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception>
        /// <filterpriority>2</filterpriority>
        /// <PermissionSet>
        ///     <IPermission
        ///         class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"
        ///         version="1" Flags="RemotingConfiguration, Infrastructure" />
        /// </PermissionSet>
        public override object InitializeLifetimeService()
        {
            // prevents proxy from expiration
            return null;
        }
    }
}*/