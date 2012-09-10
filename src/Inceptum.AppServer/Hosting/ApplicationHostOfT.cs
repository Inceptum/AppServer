using System;
using System.IO;
using System.Runtime.CompilerServices;
using Castle.Facilities.Logging;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Inceptum.AppServer.Configuration;
using Inceptum.AppServer.Logging;
using Inceptum.AppServer.Windsor;
using Inceptum.Core.Utils;

namespace Inceptum.AppServer.Hosting
{
    internal class ApplicationHost<TApp> : MarshalByRefObject, IApplicationHost where TApp : IHostedApplication
    {
        private WindsorContainer m_Container;
        private readonly HostedAppInfo m_AppInfo;

        public HostedAppInfo AppInfo
        {
            get { return m_AppInfo; }
        }

        internal WindsorContainer Container
        {
            get { return m_Container; }
        }


        public ApplicationHost(HostedAppInfo appInfo )
        {
            m_AppInfo = appInfo;
            Status=HostedAppStatus.NotStarted;
        }

        #region IApplicationHost Members

        public HostedAppStatus Status { get; private set; }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Start(IConfigurationProvider configurationProvider, AppServerContext context)
        {
            Status = HostedAppStatus.Starting;
            if (m_Container != null)
                throw new InvalidOperationException("Host is already started");
            try
            {
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
                    //.AddFacility<LoggingFacility>(f => f.LogUsing(LoggerImplementation.NLog).WithConfig("nlog.config"))
                    .AddFacility<LoggingFacility>(f => f.LogUsing<GenericsAwareNLoggerFactory>().WithConfig(Path.Combine(context.BaseDirectory,"nlog.config")))
                    .Register(
                        Component.For<AppServerContext>().Instance(context),
                        Component.For<IConfigurationProvider>().Instance(configurationProvider)
                    )
                    .Install(FromAssembly.Instance(typeof (TApp).Assembly, new PluginInstallerFactory()))
                    .Register(Component.For<IHostedApplication>().ImplementedBy<TApp>())
                    .Resolve<IHostedApplication>().Start();
                Status = HostedAppStatus.Started;
            }
            catch (Exception e)
            {
                Status = HostedAppStatus.NotStarted;

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
            Status = HostedAppStatus.Stopping;
            if (m_Container == null)
                throw new InvalidOperationException("Host is not started");
            m_Container.Dispose();
            m_Container = null;
            Status = HostedAppStatus.NotStarted;
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