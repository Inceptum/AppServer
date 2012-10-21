using System.Collections.Generic;
using Inceptum.AppServer.Configuration.Model;

namespace Inceptum.AppServer.Configuration
{

    public enum BundleAction
    {
        None,
        Save,
        Create,
        Delete
    }

    public class BundleData
    {
        public BundleAction Action { get; set; }
        public string Configuration { get; set; }
        public string Name { get; set; }
        public string Content { get; set; }
    }


    public interface IConfigurationPersister
    {
        BundleData[] Load();
        void Save(IEnumerable<BundleData> data);
        string Create(string name);
        bool Delete(string name);
    }
}
