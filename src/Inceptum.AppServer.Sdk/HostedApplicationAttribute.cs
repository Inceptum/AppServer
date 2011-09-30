using System;

namespace Inceptum.AppServer
{
    /// <summary>
    /// Attribute used to identify hosted applications
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class HostedApplicationAttribute:Attribute
    {
        /// <summary>
        /// Creates new instance of HostedApplicationAttribute
        /// </summary>
        /// <param name="name">Hosted application name</param>
        public HostedApplicationAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        /// Hosted application name
        /// </summary>
        public string Name { get; private  set; }
    }
}