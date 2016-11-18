using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    public class ApplicationProjectSystem : PhysicalFileSystem, IProjectSystem, IFileSystem
    {
        private const string BIN_DIR = "bin";
        private static FrameworkName m_FrameworkName;

        static ApplicationProjectSystem()
        {
            var list = Assembly.GetExecutingAssembly().GetCustomAttributes(true);
            var attribute = list.OfType<TargetFrameworkAttribute>().First();
            m_FrameworkName = new FrameworkName(attribute.FrameworkName);

        }
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

        public FrameworkName TargetFramework => m_FrameworkName;

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


        public override void DeleteFile(string path)
        {
            base.DeleteFile(path);
        }

        public override void DeleteFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            base.DeleteFiles(files, rootDir);
        }

        public override void DeleteDirectory(string path)
        {
            base.DeleteDirectory(path);
        }

        public override void DeleteDirectory(string path, bool recursive)
        {
            base.DeleteDirectory(path, recursive);
        }
    }
}