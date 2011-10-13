using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Inceptum.AppServer
{
     [Serializable]
    internal class AppDomainInitializer : MarshalByRefObject
    {
        private Dictionary<string, Assembly> m_LoadedAssemblies;
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr LoadLibrary(string lpFileName);

        public void Initialize(string currentDir,string[] assembliesToLoad, string[] nativeDllToLoad)
        {
             var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetName().FullName);
             m_LoadedAssemblies = assembliesToLoad
                 .Where(a => !loadedAssemblies.Contains(AssemblyName.GetAssemblyName(a).FullName))
                 .Select(Assembly.LoadFrom)
                 .ToDictionary(a => a.FullName);
             AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
             Environment.CurrentDirectory = currentDir;

             foreach (var dll in nativeDllToLoad)
             {
                 if (LoadLibrary(dll) == IntPtr.Zero)
                 {
                     throw new InvalidOperationException("Failed to load unmanaged dll " + Path.GetFileName(dll) + " from package");
                 }

             }
         }

        private Assembly onAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            Assembly assembly = loadedAssemblies.Where(a => a.FullName == args.Name || a.GetName().Name == args.Name).FirstOrDefault();

            if (assembly==null && !m_LoadedAssemblies.TryGetValue(args.Name, out assembly))
            {
                assembly = m_LoadedAssemblies.Values.FirstOrDefault(a => a.GetName().Name == args.Name);
            }
            return assembly;
        }


        /// <summary>
        /// Obtains a lifetime service object to control the lifetime policy for this instance.
        /// </summary>
        /// <returns>
        /// An object of type <see cref="T:System.Runtime.Remoting.Lifetime.ILease"/> used to control the lifetime policy for this instance. This is the current lifetime service object for this instance if one exists; otherwise, a new lifetime service object initialized to the value of the <see cref="P:System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime"/> property.
        /// </returns>
        /// <exception cref="T:System.Security.SecurityException">The immediate caller does not have infrastructure permission. </exception><filterpriority>2</filterpriority><PermissionSet><IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="RemotingConfiguration, Infrastructure"/></PermissionSet>
        public override object InitializeLifetimeService()
        {
            // prevents proxy from expiration
            return null;
        }
    }
}