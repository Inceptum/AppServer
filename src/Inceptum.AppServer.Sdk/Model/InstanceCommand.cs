using System;
using System.Collections.Generic;

namespace Inceptum.AppServer.Hosting
{
    [Serializable]
    public class InstanceCommandParam
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
    }
    [Serializable]
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

        public InstanceCommandParam[] Parameters { get; set; }
        public string Name { get; set; }
    }
}