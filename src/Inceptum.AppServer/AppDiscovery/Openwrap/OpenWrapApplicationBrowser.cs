using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Castle.Components.DictionaryAdapter;
using Castle.Core.Logging;
using OpenFileSystem.IO.FileSystems.InMemory;
using OpenFileSystem.IO.FileSystems.Local.Win32;
using OpenWrap.PackageManagement;
using OpenWrap.PackageManagement.Exporters;
using OpenWrap.PackageManagement.Exporters.Assemblies;
using OpenWrap.PackageManagement.Exporters.Commands;
using OpenWrap.PackageModel;
using OpenWrap.Repositories;
using OpenWrap.Repositories.FileSystem;
using OpenWrap.Runtime;
using OpenWrap.Services;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
{
    public class OpenWrapApplicationBrowser : IApplicationBrowser
    {
        private const string DEBUG_REPO_FOLDER = "DebugRepo";
        private readonly ExecutionEnvironment m_Environment;
        private readonly IPackageExporter m_Exporter;
        private readonly IPackageManager m_PackageManager;
        private readonly IPackageRepository m_ProjectRepository;
        private readonly ServiceRegistry m_ServiceRegistry;
        private readonly IPackageRepository m_DebugRepo;
        private ILogger m_Logger;


        public OpenWrapApplicationBrowser(string repository, string[] debugWraps, ILogger logger = null)
        {
            m_Logger = logger ?? NullLogger.Instance;
            m_ServiceRegistry = new ServiceRegistry();
            m_ServiceRegistry.Override<IPackageExporter>(() => new DefaultPackageExporter(new List<IExportProvider>
                                                                                              {
                                                                                                  new DefaultAssemblyExporter(),
                                                                                                  new CecilCommandExporter(),
                                                                                                  new SolutionPluginExporter(),
                                                                                                  new NativeDllExporter(),
                                                                                                  new AppConfigExporter(),
                                                                                                  new HostedApplicationExporter()
                                                                                              }));
            m_ServiceRegistry.Initialize();


             
            if (Directory.Exists(DEBUG_REPO_FOLDER))
                Directory.Delete(DEBUG_REPO_FOLDER, true);
            if (debugWraps.Any())
            {
                Directory.CreateDirectory(DEBUG_REPO_FOLDER);
                foreach (var wrap in debugWraps)
                {
                    File.Copy(wrap,Path.Combine(Path.GetFullPath(DEBUG_REPO_FOLDER),Path.GetFileName(wrap)));
                }
                m_DebugRepo = new FolderRepository(new Win32Directory(DEBUG_REPO_FOLDER));
            }
       

           
            m_Exporter = ServiceLocator.GetService<IPackageExporter>();
            //m_RemoteRepository = getRepository(remoteRepository);

            m_PackageManager = ServiceLocator.GetService<IPackageManager>();
            m_Environment = new ExecutionEnvironment
                                {
                                    Platform = (IntPtr.Size == 4) ? "x86" : "x64",
                                    Profile = (Environment.Version.Major >= 4) ? "net40" : "net35"
                                };
            string localPath = Path.GetFullPath(repository);
            if (!Directory.Exists(localPath))
                Directory.CreateDirectory(localPath);
            m_ProjectRepository = new FolderRepository(new Win32Directory(localPath));
        }

        #region IApplicationBrowser Members

        public IEnumerable<HostedAppInfo> GetAvailabelApps()
        {

            var debugApps = m_DebugRepo != null
                                ? m_PackageManager.GetSystemExports<IHostedApplicationExport>(m_DebugRepo, m_Environment).SelectMany(x => x).Select(getAppLoadParams).ToArray()
                                : new HostedAppInfo[0];

            var packages = m_ProjectRepository.PackagesByName.SelectMany(x => x);

            var releaseApps = new List<HostedAppInfo>();
            foreach (var package in packages)
            {
                var descriptor = new PackageDescriptor {Name = "tester", Version = new Version(0, 0, 1, 1)};
                descriptor.Dependencies.Add(new PackageDependency(package.Name, new[] { new EqualVersionVertex(package.Version) }));
                releaseApps.AddRange(m_PackageManager.GetProjectExports<IHostedApplicationExport>(descriptor, m_ProjectRepository, m_Environment)
                    .SelectMany(x => x).Where(x => !debugApps.Any(p => p.Name == x.Name)).Select(getAppLoadParams));
            }

            
      /*      var releaseApps = m_PackageManager.GetSystemExports<IHostedApplicationExport>(m_ProjectRepository, m_Environment)
                                            .SelectMany(x => x).Where(x => !debugApps.Any(p => p.Name == x.Name)).Select(getAppLoadParams).ToArray();
    */
            return debugApps.Concat(releaseApps.GroupBy(a => a.Version).Select(g => g.First())).ToArray();
        }

        #endregion

        private static IPackageRepository getRepository(string path)
        {
            return Directory.GetFiles(path).Any(f => Path.GetFileName(f).ToLower() == "index.wraplist")
                       ? (IPackageRepository) new IndexedFolderRepository("", new Win32Directory(Path.GetFullPath(path)))
                       : new FolderRepository(new Win32Directory(Path.GetFullPath(path)));
        }

        private HostedAppInfo getAppLoadParams(IHostedApplicationExport appExport)
        {
            IEnumerable<IPackageInfo> packages = new[] {appExport.Package}.Concat(
                ServiceLocator.GetService<IPackageResolver>().TryResolveDependencies(appExport.Package.Descriptor, new[] {m_ProjectRepository}).SuccessfulPackages.Select(_ => _.Packages.First())
                );
            IEnumerable<Exports.IAssembly> assembliesTooLoad = packages.SelectMany(p => m_Exporter.Exports<Exports.IAssembly>(p.Load(), m_Environment))
                                                        .SelectMany(e => e.Select(f => f));

            var appConfig = packages.SelectMany(p => m_Exporter.Exports<IAppConfig>(p.Load(), m_Environment))
                .SelectMany(files => files.Select(f=>f)).FirstOrDefault();

            IEnumerable<string> nativeDllsToLoad = packages.SelectMany(p => m_Exporter.Exports<INativeDll>(p.Load(), m_Environment)).SelectMany(e => e.Select(f => f.File.Path.FullPath));

            

            return new HostedAppInfo(appExport.Name,appExport.Vendor, appExport.Version, appExport.Type,
                                     assembliesTooLoad.ToDictionary(a => a.AssemblyName, a => a.File.Path.FullPath), nativeDllsToLoad)
                       {
                           ConfigFile = appConfig == null ? null : appConfig.File.Path.FullPath,
                           Vendor=appExport.Vendor,Description = appExport.Vendor+"© "+appExport.Name+" v"+appExport.Version
                           
                       };
        }
    }
}