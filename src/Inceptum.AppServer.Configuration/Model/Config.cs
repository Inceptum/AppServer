using System;
using System.Collections.Generic;
using System.Linq;

namespace Inceptum.AppServer.Configuration.Model
{
    interface IBundleObserver
    {
        void OnChildCreated(Bundle parent,Bundle child);
    }

    public class Config  : BundleCollectionBase, IBundleObserver
    {
        private readonly SortedDictionary<string,Bundle> m_Map=new SortedDictionary<string, Bundle>(StringComparer.InvariantCultureIgnoreCase); 

        public IEnumerable<Bundle> Bundles
        {
            get
            {
                return m_Map.Values.ToArray();
            }
        } 

        public Config(IContentProcessor contentProcessor, string name) : base(contentProcessor, name)
        {
        }

        protected override Bundle CreateSubBundle(string name)
        {
            if (!ValidationHelper.IsValidBundleName(name))
            {
                throw new ArgumentException(ValidationHelper.INVALID_NAME_MESSAGE, "name");
            }
            name = name.ToLower();
            var bundle = new Bundle(ContentProcessor, this,name);
            m_Map.Add(bundle.Name, bundle);
            return bundle;
        }


        public Bundle this[string name]
        {
            get
            {
                Bundle b;
                m_Map.TryGetValue(name.ToLower(), out b);
                return b;
            }
        }
        public void OnChildCreated(Bundle parent, Bundle child)
        {
            m_Map.Add(child.Name,child);
        }

    }
}