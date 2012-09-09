using System;

namespace Inceptum.AppServer
{
    /// <summary>
    /// Attribute used to identify hosted applications
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class HostedApplicationAttribute:Attribute
    {
        public const string DEFAULT_VENDOR="Unknown";

        /// <summary>
        /// Creates new instance of HostedApplicationAttribute
        /// </summary>
        /// <param name="name">Hosted application name</param>
        /// <param name="vendor"></param>
        public HostedApplicationAttribute(string name, string vendor)
        {
            Name = name;
            Vendor = vendor;
        }       
        
        public HostedApplicationAttribute(string name)
        {
            Name = name;
            Vendor = DEFAULT_VENDOR;
        }

        /// <summary>
        /// Hosted application name
        /// </summary>
        public string Name { get; private  set; }

        /// <summary>
        /// Hosted application name
        /// </summary>
        public string Vendor { get; private  set; }
    }
}