using System;
using System.Runtime.Serialization;

namespace Inceptum.AppServer
{
    [Serializable]
    [DataContract]
    public class AppServerContext
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string AppsDirectory { get; set; }
        [DataMember]
        public string BaseDirectory { get; set; }
    }
}