﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NuGet;

namespace Inceptum.AppServer
{
   

    [Serializable]
    public class HostedAppInfo
    {


        public HostedAppInfo(string name,string vendor, SemanticVersion version,string appType, IDictionary<AssemblyName, string> assembliesToLoad, IEnumerable<string> nativeDllToLoad = null)
        {
            AssembliesToLoad = new Dictionary<AssemblyName, string>(assembliesToLoad);
            NativeDllToLoad = (nativeDllToLoad??new string[0]).ToArray();
            AppType = appType;

            Name = name;
            Version = version;
            Vendor = vendor;
        }

        public HostedAppInfo(string name, string vendor, SemanticVersion version)
        {
            Name = name;
            Version = version;
            Vendor = vendor;
        }

        public Dictionary<AssemblyName, string> AssembliesToLoad { get; private set; }
        public string Browser { get; set; }
        public string Name { get; set; }
        public string Vendor { get; set; }
        public string Description { get; set; }
        public string AppType { get; set; }
        public string[] ConfigFiles { get; set; }
        public SemanticVersion Version { get; set; }

        public string[] NativeDllToLoad { get; private set; }
        public bool Debug { get; set; }

        public override string ToString()
        {
            return Name + " v" + Version;
        }
    }
}
