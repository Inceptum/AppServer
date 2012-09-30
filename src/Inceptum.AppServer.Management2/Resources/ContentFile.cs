using System;
using System.IO;
using System.Linq;
using OpenRasta.IO;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management2.Resources
{
    public class ContentFile : IFile
    {
        private string m_Key;
        private Stream m_Stream;

        public ContentFile(string key)
        {
            m_Key = key;
            m_Stream = getResourceStream(key);
            if (m_Stream == null)
            {
                throw new FileNotFoundException(string.Format("Resource with key {0} not found",key));                
            }
        }

        public ContentFile(string folder,string path)
        {
            m_Key = path;
            m_Stream = new MemoryStream(File.ReadAllBytes(Path.Combine(folder,path)));
            if(m_Stream==null)
                throw new FileNotFoundException(string.Format("Resource with key {0} not found",path));
        }


        public Stream OpenStream()
        {

            m_Stream.Seek(0, SeekOrigin.Begin);
            var stream = new MemoryStream();
            m_Stream.CopyTo(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public MediaType ContentType
        {
            get
            {
                switch (Path.GetExtension(m_Key).ToLower())
                {
                    case ".css":
                        return new MediaType("text/css");
                    case ".js":
                        return new MediaType("text/javascript");
                    case ".png":
                        return new MediaType("image/png");
                    case ".gif":
                        return new MediaType("image/gif");
                    default:
                        return MediaType.ApplicationOctetStream;
                }
            }
        }

        public string FileName
        {
            get { return m_Key; }
        }

        public long Length
        {
            get {              
                return m_Stream.Length;
            }
        }

        private Stream getResourceStream(string name)
        {
            var assembly = GetType().Assembly;
            var stream = assembly.GetManifestResourceStream(name);
            if (stream != null) return stream;
            
            //Try get resource ignoring case
            name = assembly.GetManifestResourceNames().SingleOrDefault(x => x.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            return name == null ? null : assembly.GetManifestResourceStream(name);
        }
    }
}

