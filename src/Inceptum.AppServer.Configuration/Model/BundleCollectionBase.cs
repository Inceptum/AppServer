using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Inceptum.AppServer.Configuration.Model
{
    public abstract class BundleCollectionBase : IEnumerable<Bundle>
    {
        private readonly Dictionary<string, Bundle> m_ChildBundles = new Dictionary<string, Bundle>(StringComparer.InvariantCultureIgnoreCase);
        private readonly IContentProcessor m_ContentProcessor;
        private readonly string m_Name;

        protected IContentProcessor ContentProcessor
        {
            get { return m_ContentProcessor; }
        }

        public virtual string Name
        {
            get { return m_Name; }
        }

        protected BundleCollectionBase(IContentProcessor contentProcessor, string name)
        {
            if (contentProcessor == null)
                throw new ArgumentNullException("contentProcessor");
            if (!ValidationHelper.IsValidBundleName(name))
            {
                throw new ArgumentException(ValidationHelper.INVALID_NAME_MESSAGE, "name");
            }
            m_ContentProcessor = contentProcessor;
            m_Name = name.ToLower();
        }

      


        public Bundle CreateBundle(string name, string content = null)
        {
            var bundle = CreateSubBundle(name);
            AddBundle(bundle);
            if (content != null)
                bundle.Content = content;
            return bundle;

        }

        protected abstract Bundle CreateSubBundle(string name);

        internal void AddBundle(Bundle bundle)
        {
            if (this.Where(b => b.Name == bundle.Name).Any())
                throw new ArgumentException("There is already a bundle with the same name", "bundle");
            m_ChildBundles.Add(bundle.Name, bundle);
        }

        public IEnumerator<Bundle> GetEnumerator()
        {
            return m_ChildBundles.Select(m => m.Value).ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}