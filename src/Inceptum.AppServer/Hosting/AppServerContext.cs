using System;

namespace Inceptum.AppServer
{
    [Serializable]
    public class AppServerContext
    {
        public string Name { get; set; } 
        public string AppsDirectory { get; set; }
        public string BaseDirectory { get; set; }
    }
}