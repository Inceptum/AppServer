using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Inceptum.AppServer.Hosting
{
    [Serializable]
    [DataContract]
    public class InstanceCommandParam
    {
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string Type { get; set; }
        [DataMember]
        public string Value { get; set; }
    }


    [Serializable]
    [DataContract]
    public class InstanceCommand
    {

        public InstanceCommand(string name,InstanceCommandParam[] parameters)
        {
            Parameters = parameters;
            Name = name;
        }

        public InstanceCommand()
        {
        }

        [DataMember]
        public InstanceCommandParam[] Parameters { get; set; }
        [DataMember]
        public string Name { get; set; }
    }
}