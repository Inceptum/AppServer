using System.IO;
using OpenRasta.IO;
using OpenRasta.Web;

namespace Inceptum.AppServer.Management.Resources
{
    public class ContentFile : IFile
    {
        private string m_Key;
        private Stream m_Stream;

        public ContentFile(string key)
        {
            m_Key = key;
            m_Stream = GetType().Assembly.GetManifestResourceStream(m_Key);
        }


        public Stream OpenStream()
        {
            return m_Stream;
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
    }
}
