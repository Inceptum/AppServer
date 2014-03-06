using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    public class ApplicationProjectSystem : PhysicalFileSystem, IProjectSystem, IFileSystem
    {
        public ApplicationProjectSystem(string root)
            : base(root)
        {
        }

        private const string BIN_DIR = "bin";

        public string ProjectName
        {
            get
            {
                return  Root;
            }
        }

        public bool IsBindingRedirectSupported { get; private set; }

        public bool FileExistsInProject(string path)
        {
            return this.FileExists(path);
        }

        public FrameworkName TargetFramework
        {
            get
            {
                return new FrameworkName(".NETFramework,Version=v4.5");
            }
        }


        public void AddReference(string referencePath, Stream stream)
        {
            string fileName = Path.GetFileName(referencePath);
            string fullPath = this.GetFullPath(GetReferencePath(fileName));
/*
            //nuget provides empty stream for references so this is the only way to get the file
            using (var fs = new FileStream(Path.Combine(Root, "packages", referencePath), FileMode.Open))
*/
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
/*
            if (!path.StartsWith("tools", StringComparison.OrdinalIgnoreCase))
                return !Path.GetFileName(path).Equals("app.config", StringComparison.OrdinalIgnoreCase);
*/
            return true;
          
        }

        public string ResolvePath(string path)
        {
            return Path.Combine("Content",path);
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