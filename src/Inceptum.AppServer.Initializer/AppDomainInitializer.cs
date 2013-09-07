using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Inceptum.AppServer.Initializer
{

    [Serializable]
    public class AppDomainCrashHandler : MarshalByRefObject
    {
        private readonly Action<string> m_Handler;

        public AppDomainCrashHandler(Action<string> handler)
        {
            m_Handler = handler;
        }

        public void Handle(string exception)
        {
            m_Handler(exception);
        }
    }

    /// <summary>
    /// Initializes app domain with explicit list of assemblies resolvable assemblies. Class is taken off to separate assembly
    /// to workaround clr assembly resolving in load from context behaviour where refereneced assembluies are loaded from  the
    /// same loacation as referencing assembly (otherwise AssemblyResolve does not fire for assemblies referenced by assembly 
    /// referenced by AppDomainInitializer declaring assembly. Assemblies are taken from AppDomainInitializer declaring assembly
    /// location and there is no chance to provide the ones from explicit list. As result appdomain may load wrong assembly versions ). 
    /// </summary>
    [Serializable]
    public class AppDomainInitializer : MarshalByRefObject
    {
        private Dictionary<AssemblyName, Lazy<Assembly>> m_LoadedAssemblies;

        public AppDomainInitializer()
        {
            AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        public void Initialize(string workingDirectory, Dictionary<AssemblyName, string> assembliesToLoad, IEnumerable<string> nativeDllToLoad, AppDomainCrashHandler crashHandler)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => crashHandler.Handle(args.ExceptionObject.ToString());
            IEnumerable<AssemblyName> loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName());

            m_LoadedAssemblies = assembliesToLoad.Where(asm => loadedAssemblies.All(a => a.Name != asm.Key.Name))
                                                 .ToDictionary(asm => asm.Key, asm => new Lazy<Assembly>(() => loadAssembly(asm.Value)));
      
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


        public object CreateInstance(string typeName, params string[] typeArguments)
        {
            Type type = Type.GetType(typeName);
            if (type.IsGenericTypeDefinition)
                type=type.MakeGenericType(typeArguments.Select(Type.GetType).ToArray());
            return Activator.CreateInstance(type);
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
}