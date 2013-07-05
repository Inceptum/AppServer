using System;
using System.Collections.Generic;

namespace Inceptum.AppServer.Hosting
{
    [Serializable]
    public class InstanceCommandParam
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }
    [Serializable]
    public class InstanceCommandSpec
    {
        public InstanceCommandSpec(string name,InstanceCommandParam[] parameters)
        {
            Parameters = parameters;
            Name = name;
        }

        public InstanceCommandParam[] Parameters { get; private set; }
        public string Name { get; private set; }
    }
}