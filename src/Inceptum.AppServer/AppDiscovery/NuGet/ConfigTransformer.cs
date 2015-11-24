using System.Collections.Generic;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    internal class ConfigTransformer : IPackageFileTransformer
    {
        private readonly string m_Extension;

        public ConfigTransformer(string extension)
        {
            m_Extension = extension;
        }

        public void TransformFile(IPackageFile file, string targetPath, IProjectSystem projectSystem)
        {
            var path = targetPath + m_Extension;
            if (path.ToLower().StartsWith("config\\"))
                path = path.Substring(7);
            if (projectSystem.FileExists(path))
                projectSystem.AddFile(path + ".default", file.GetStream());
            else
                projectSystem.AddFile(path, file.GetStream());
        }

        public void RevertFile(IPackageFile file, string targetPath, IEnumerable<IPackageFile> matchingFiles, IProjectSystem projectSystem)
        {
        }
    }
}