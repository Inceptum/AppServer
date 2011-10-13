using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Castle.DynamicProxy;
using Castle.Facilities.Logging;
using Castle.Facilities.TypedFactory;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Inceptum.AppServer.Configuration;
using Inceptum.Core.Utils;

namespace Inceptum.AppServer
{

        class ApplicationHostProxy : IApplicationHost
    {
        private readonly AppDomain m_Domain;
        private readonly IApplicationHost m_ApplicationHost;

     

        public ApplicationHostProxy(IApplicationHost applicationHost,AppDomain domain)
        {
            if (applicationHost == null) throw new ArgumentNullException("applicationHost");
            if (domain == null) throw new ArgumentNullException("domain");
            m_ApplicationHost = applicationHost;
            m_Domain = domain;
        }


        public void Start(IConfigurationProvider configurationProvider, AppServerContext context)
        {
            m_ApplicationHost.Start(configurationProvider,  context);
        }

        public void Stop()
        {
            m_ApplicationHost.Stop();
            AppDomain.Unload(m_Domain);
        }
    }


    [Serializable]
    internal class ApplicationHost : MarshalByRefObject
    {
        private Dictionary<string, Assembly> m_LoadedAssemblies;
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr LoadLibrary(string lpFileName);

        public static IApplicationHost Create(HostedAppInfo appInfo)
        {
            AppDomain domain = AppDomain.CreateDomain(appInfo.Name, null, new AppDomainSetup
                                                                              {
                                                                                  ApplicationBase = AppDomain.CurrentDomain.BaseDirectory,
                                                                                  //ApplicationBase = appInfo.BaseDirectory,
                                                                                  PrivateBinPathProbe = null,
                                                                                  DisallowApplicationBaseProbing = true,
                                                                                  //TODO: use plugin app.config
                                                                                  ConfigurationFile = AppDomain.CurrentDomain.SetupInformation.ConfigurationFile
                                                                                  // appInfo.ConfigFile
                                                                              });
  
            var applicationHost = (ApplicationHost)domain.CreateInstanceFromAndUnwrap(typeof(ApplicationHost).Assembly.Location, typeof(ApplicationHost).FullName, false, BindingFlags.Default, null, null, null, null);
            return new ApplicationHostProxy(applicationHost.load(appInfo),domain);
        }


        private IApplicationHost load(HostedAppInfo appInfo)
        {
            var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetName().FullName);
            m_LoadedAssemblies = appInfo.AssembliesToLoad
                .Where(a => !loadedAssemblies.Contains(AssemblyName.GetAssemblyName(a).FullName))
                .Select(Assembly.LoadFrom)
                .ToDictionary(a => a.FullName);
            AppDomain.CurrentDomain.AssemblyResolve += onAssemblyResolve;
            Environment.CurrentDirectory = appInfo.BaseDirectory;

            foreach (var dll in appInfo.NativeDllToLoad)
            {
                if(LoadLibrary(dll)==IntPtr.Zero)
                {
                    throw new InvalidOperationException("Failed to load unmanaged dll "+Path.GetFileName(dll)+" from package");
                }

            }

            var hostType = typeof(ApplicationHost<>).MakeGenericType(Type.GetType(appInfo.AppType));
            return (IApplicationHost)Activator.CreateInstance(hostType);
        }

        private Assembly onAssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly assembly;
            if (!m_LoadedAssemblies.TryGetValue(args.Name, out assembly))
            {
                assembly = m_LoadedAssemblies.Values.FirstOrDefault(a => a.GetName().Name == args.Name);
            }
            return assembly;
        }
    }

    internal class ApplicationHost<TApp> : MarshalByRefObject, IApplicationHost where TApp : IHostedApplication
    {
        private WindsorContainer m_Container;

        internal WindsorContainer Container
        {
            get { return m_Container; }
        }

        #region IApplicationHost Members

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start(IConfigurationProvider configurationProvider, AppServerContext context)
        {
            if (m_Container != null)
                throw new InvalidOperationException("Host is already started");
            try
            {
                Environment.CurrentDirectory = Path.GetDirectoryName(GetType().Assembly.Location);
                AppDomainRenderer.Register();

                m_Container = new WindsorContainer();
                //castle config
                string configurationFile = string.Format("castle.{0}.config", AppDomain.CurrentDomain.FriendlyName);
                if (File.Exists(configurationFile))
                {
                    m_Container
                        .Install(Castle.Windsor.Installer.Configuration.FromXmlFile(configurationFile));
                }


                m_Container
                    .AddFacility<LoggingFacility>(f => f.LogUsing(LoggerImplementation.NLog).WithConfig("nlog.config"))
                    .Register(
                            Component.For<AppServerContext>().Instance(context),
                            Component.For<IConfigurationProvider>().Instance(configurationProvider)
                            )
                    .Install( FromAssembly.Instance(typeof(TApp).Assembly,new PluginInstallerFactory()))
                    .Register(Component.For<IHostedApplication>().ImplementedBy<TApp>())
                    .Resolve<IHostedApplication>().Start();
            }
            catch (Exception e)
            {
                try
                {
                    if (m_Container != null)
                    {
                        m_Container.Dispose();
                        m_Container = null;
                    }
                }
                catch (Exception e1)
                {
                    throw new ApplicationException(
                        string.Format("Failed to start: {0}{1}Exception while disposing container: {2}", e,
                                      Environment.NewLine, e1));
                }
                throw new ApplicationException(string.Format("Failed to start: {0}", e));
            }
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Stop()
        {
            if (m_Container == null)
                throw new InvalidOperationException("Host is not started");
            m_Container.Dispose();
            m_Container = null;
        }

        #endregion

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