using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    public class ApplicationProjectSystem : PhysicalFileSystem, IProjectSystem, IFileSystem
    {
        private const string BIN_DIR = "bin";

        public ApplicationProjectSystem(string root)
            : base(root)
        {
        }

        public string ProjectName
        {
            get { return Root; }
        }

        public bool IsBindingRedirectSupported { get; private set; }

        public bool FileExistsInProject(string path)
        {
            return FileExists(path);
        }

        public FrameworkName TargetFramework
        {
            get { return new FrameworkName(".NETFramework,Version=v4.5"); }
        }

        public void AddReference(string referencePath, Stream stream)
        {
            var fullPath = GetFullPath(GetReferencePath(referencePath));
            AddFile(fullPath, stream);
        }

        public void AddFrameworkReference(string name)
        {
        }

        public object GetPropertyValue(string propertyName)
        {
            if (propertyName == null)
                return null;
            if (propertyName.Equals("RootNamespace", StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            return null;
        }

        public bool IsSupportedFile(string path)
        {
            return true;
        }

        public string ResolvePath(string path)
        {
            if (path.ToLower().StartsWith("config\\"))
                return path.Substring(7);
            return Path.Combine("Content", path);
        }

        public void AddImport(string targetPath, ProjectImportLocation location)
        {
            throw new NotImplementedException();
        }

        public void RemoveImport(string targetPath)
        {
            throw new NotImplementedException();
        }

        public bool ReferenceExists(string name)
        {
            return FileExists(GetReferencePath(name));
        }

        public void RemoveReference(string name)
        {
            DeleteFile(GetReferencePath(name));
            if (this.GetFiles(BIN_DIR, "*.*").Any())
                return;
            DeleteDirectory(BIN_DIR);
        }

        protected virtual string GetReferencePath(string name)
        {
            return Path.Combine(BIN_DIR, name);
        }
    }
}