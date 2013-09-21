using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Inceptum.AppServer.Configuration.Model
{
    internal interface IBundleEventTracker
    {
        Bundle CreateBundle(Bundle parent, string name);
        void UpdateBundle(Bundle bundle);
        void DeleteBundle(Bundle bundle);
    }

    public class Config : BundleCollectionBase, IBundleEventTracker
    {
        private readonly IConfigurationPersister m_Persister;
        private readonly List<BundleData> m_UncommitedEvents = new List<BundleData>();
        private SortedDictionary<string, string> m_Map;

        public Config(IConfigurationPersister persister, IContentProcessor contentProcessor, string name, Dictionary<string, string> bundles = null)
            : base(contentProcessor, null, name)
        {
            m_Map = new SortedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            m_Persister = persister;
            if (bundles == null)
                return;
            foreach (var bundleContent in bundles)
            {
                this[bundleContent.Key] = bundleContent.Value;
            }
            populateMap();
            m_UncommitedEvents.Clear();
        }


        public string this[string name]
        {
            get
            {
                string value;
                m_Map.TryGetValue(name, out value);
                return value;
            }
            set
            {
                string[] path = name.Split(new[] {'.'});
                BundleCollectionBase bundle = this;
                int i;
                for (i = 0; i < path.Length; i++)
                {
                    Bundle found = bundle.FirstOrDefault(b => b.ShortName.ToLower() == path[i].ToLower());
                    if (found == null)
                        break;
                    bundle = found;
                }
                bundle = path.Skip(i).Aggregate(bundle, (current, t) => current.CreateSubBundle(t));
                (bundle as Bundle).PureContent = value ?? ContentProcessor.GetEmptyContent();
            }
        }

        Bundle IBundleEventTracker.CreateBundle(Bundle parent, string name)
        {
            name = name.ToLower();
            var bundle = new Bundle(parent ?? (BundleCollectionBase) this, name);
            lock (m_UncommitedEvents)
            {
                m_UncommitedEvents.Add(new BundleData {Configuration = Name, Name = bundle.Name, Content = bundle.PureContent, Action = BundleAction.Create});
            }
            return bundle;
        }

        void IBundleEventTracker.UpdateBundle(Bundle bundle)
        {
            lock (m_UncommitedEvents)
            {
                m_UncommitedEvents.Add(new BundleData { Configuration = Name, Name = bundle.Name, Content = bundle.PureContent, Action = BundleAction.Save });
            }
        }

        void IBundleEventTracker.DeleteBundle(Bundle bundle)
        {
            lock (m_UncommitedEvents)
            {
                m_UncommitedEvents.Add(new BundleData { Configuration = Name, Name = bundle.Name, Content = bundle.PureContent, Action = BundleAction.Delete });
            }
        }


        public bool Commit()
        {
            try
            {
                lock (m_UncommitedEvents)
                {
                    m_Persister.Save(m_UncommitedEvents.ToArray());
                    populateMap();
                    return true;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                m_UncommitedEvents.Clear();
            }
        }

        private void populateMap()
        {
            var map = new SortedDictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
            Visit(b => map[b.Name] = b.Content);
            Interlocked.Exchange(ref m_Map, map);
        }
    }
}