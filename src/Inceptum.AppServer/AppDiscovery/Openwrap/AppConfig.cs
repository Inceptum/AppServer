using OpenFileSystem.IO;
using OpenWrap.PackageModel;

namespace Inceptum.AppServer.AppDiscovery.Openwrap
{
    internal class AppConfig : IAppConfig
    {
        public AppConfig(string path, IPackage package, IFile file)
        {
            Path = path;
            Package = package;
            File = file;
        }

        #region INativeDll Members

        public string Path { get; private set; }
        public IPackage Package { get; private set; }

        public IFile File { get; private set; }

        #endregion
    }
}