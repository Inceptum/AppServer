using System;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using NuGet;

namespace Inceptum.AppServer.AppDiscovery.NuGet
{
    public class InMemoryPackagesConfigFileSystem:IFileSystem
    {
        private byte[] m_Bytes;

        public InMemoryPackagesConfigFileSystem(Stream packagesConfig)
        {
            m_Bytes = packagesConfig.ReadAllBytes();
        }

        public void DeleteDirectory(string path, bool recursive)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<string> GetDirectories(string path)
        {
            throw new NotImplementedException();
        }

        public string GetFullPath(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteFile(string path)
        {
            throw new NotImplementedException();
        }

        public void DeleteFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            throw new NotImplementedException();
        }

        public bool FileExists(string path)
        {
            return path == "packages.config";
        }

        public bool DirectoryExists(string path)
        {
            throw new NotImplementedException();
        }

        public void AddFile(string path, Stream stream)
        {
            throw new NotImplementedException();
        }

        public void AddFile(string path, Action<Stream> writeToStream)
        {
            throw new NotImplementedException();
        }

        public void AddFiles(IEnumerable<IPackageFile> files, string rootDir)
        {
            throw new NotImplementedException();
        }

        public void MakeFileWritable(string path)
        {
            throw new NotImplementedException();
        }

        public void MoveFile(string source, string destination)
        {
            throw new NotImplementedException();
        }

        public Stream CreateFile(string path)
        {
            throw new NotImplementedException();
        }

        public Stream OpenFile(string path)
        {
           if(path=="packages.config")
               return new MemoryStream(m_Bytes);
           throw new NotImplementedException();
        }

        public DateTimeOffset GetLastModified(string path)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset GetCreated(string path)
        {
            throw new NotImplementedException();
        }

        public DateTimeOffset GetLastAccessed(string path)
        {
            throw new NotImplementedException();
        }

        public ILogger Logger { get; set; }
        public string Root { get; private set; }
    }
}