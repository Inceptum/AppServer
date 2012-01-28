using System;
using System.Configuration;
using System.Runtime.Serialization;
using System.Xml;

namespace Inceptum.AppServer.Configuration
{
    public class BundleNotFoundException : ConfigurationErrorsException
    {
        public BundleNotFoundException(string message, Exception inner, string filename, int line) : base(message, inner, filename, line)
        {
        }

        public BundleNotFoundException()
        {
        }

        public BundleNotFoundException(string message) : base(message)
        {
        }

        public BundleNotFoundException(string message, Exception inner) : base(message, inner)
        {
        }

        public BundleNotFoundException(string message, string filename, int line) : base(message, filename, line)
        {
        }

        public BundleNotFoundException(string message, XmlNode node) : base(message, node)
        {
        }

        public BundleNotFoundException(string message, Exception inner, XmlNode node) : base(message, inner, node)
        {
        }

        public BundleNotFoundException(string message, XmlReader reader) : base(message, reader)
        {
        }

        public BundleNotFoundException(string message, Exception inner, XmlReader reader) : base(message, inner, reader)
        {
        }

        protected BundleNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}