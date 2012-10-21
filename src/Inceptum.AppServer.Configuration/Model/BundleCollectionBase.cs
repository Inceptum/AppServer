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
        private readonly IBundleEventTracker m_EventTracker;

        public  IContentProcessor ContentProcessor
        {
            get { return m_ContentProcessor; }
        }

        public virtual string Name
        {
            get { return m_Name; }
        }

        internal IBundleEventTracker EventTracker
        {
            get { return m_EventTracker; }
        }

        internal BundleCollectionBase(IContentProcessor contentProcessor, IBundleEventTracker eventTracker, string name)
        {
            if (contentProcessor == null)
                throw new ArgumentNullException("contentProcessor");
            eventTracker = eventTracker ?? this as IBundleEventTracker;
            if (eventTracker == null) throw new ArgumentNullException("eventTracker");


            if (!ValidationHelper.IsValidBundleName(name))
            {
                throw new ArgumentException(ValidationHelper.INVALID_NAME_MESSAGE, "name");
            }
            m_EventTracker = eventTracker;
            m_ContentProcessor = contentProcessor;
            m_Name = name.ToLower();
        }

      


        public Bundle CreateSubBundle(string name, string content = null)
        {
            if (this.Any(b => b.Name == name))
                throw new ArgumentException("There is already a bundle with the same name", "name");
            var bundle = EventTracker.CreateBundle(this as Bundle,name);
            m_ChildBundles.Add(name, bundle);
            bundle.Content = content??ContentProcessor.GetEmptyContent();
            return bundle;
        }


        public IEnumerator<Bundle> GetEnumerator()
        {
            return m_ChildBundles.Select(m => m.Value).ToList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Purge()
        {
            m_ChildBundles.Clear();
        }

        public void Delete(string name)
        {
            string[] path = name.Split(new[] { '.' });
            BundleCollectionBase parent = this;
            int i;
            if (path.Length > 1)
            {
                for (i = 0; i < path.Length - 1 && parent != null; i++)
                    parent = parent.FirstOrDefault(b => b.ShortName == path[i]);

                if (parent != null)
                    parent.Delete(path.Last());
                return;
            }

            Bundle child;
            if (!m_ChildBundles.TryGetValue(name, out child))
                return;
            child.Visit(b => EventTracker.DeleteBundle(b));
            m_ChildBundles.Remove(name);
        }

        internal void Visit(Action<BundleCollectionBase> visitor)
        {
            Action<BundleCollectionBase> visitorWrapper = null;
            visitorWrapper = bundle =>
            {
                foreach (var childBundle in bundle)
                {
                    visitorWrapper(childBundle);
                }
                visitor(bundle);
            };
            visitorWrapper(this);
        } 
        
        internal void Visit(Action<Bundle> visitor)
        {
            Action<BundleCollectionBase> visitorWrapper = null;
            visitorWrapper = collection =>
            {
                foreach (var childBundle in collection)
                {
                    visitorWrapper(childBundle);
                }
                var bundle = collection as Bundle;
                if(bundle!=null)
                    visitor(bundle);
            };
            visitorWrapper(this);
        }
    }
}
