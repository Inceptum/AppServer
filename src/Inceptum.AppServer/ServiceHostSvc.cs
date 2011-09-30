using System;
using System.ServiceProcess;

namespace Inceptum.AppServer
{
    /// <summary>
    /// Windows service implementation. 
    /// </summary>
    public partial class ServiceHostSvc : ServiceBase
    {
        private readonly Func<IDisposable> m_Bootstrapper;
        private IDisposable m_Host;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceHostSvc"/> class.
        /// </summary>
        /// <param name="bootstrapper">Func to be used to start application in service Start() method.</param>
        public ServiceHostSvc(Func<IDisposable> bootstrapper)
        {
            m_Bootstrapper = bootstrapper;
            InitializeComponent();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Start command is sent to the service by the Service Control Manager (SCM) or when the operating system starts (for a service that starts automatically). Specifies actions to take when the service starts.
        /// </summary>
        /// <param name="args">Data passed by the start command.</param>
        protected override void OnStart(string[] args)
        {
            m_Host = m_Bootstrapper();
        }

        /// <summary>
        /// When implemented in a derived class, executes when a Stop command is sent to the service by the Service Control Manager (SCM). Specifies actions to take when a service stops running.
        /// </summary>
        protected override void OnStop()
        {
            m_Host.Dispose();
        }
    }
}
