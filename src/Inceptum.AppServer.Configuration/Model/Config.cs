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

    public class Config  : BundleCollectionBase, IBundleEventTracker
    {
        private SortedDictionary<string, string> m_Map;
        readonly List<BundleData> m_UncommitedEvents = new List<BundleData>();
        private readonly IConfigurationPersister m_Persister;

        public Config(IConfigurationPersister persister, IContentProcessor contentProcessor, string name, Dictionary<string, string> bundles=null) 
            : base(contentProcessor,null, name)
        {
            m_Persister = persister;
            if (bundles == null)
                return;
            m_Map=new SortedDictionary<string, string>(bundles);
            reset();
        }

       

        private void reset()
        {
            Purge();
            foreach (var bundleContent in m_Map)
            {
                this[bundleContent.Key]=bundleContent.Value;
            }
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
                var path = name.Split(new[] { '.' });
                BundleCollectionBase bundle = this;
                int i;
                for (i = 0; i < path.Length; i++)
                {
                    var found = bundle.FirstOrDefault(b => b.ShortName.ToLower() == path[i].ToLower());
                    if (found == null)
                        break;
                    bundle = found;
                }
                bundle = path.Skip(i).Aggregate(bundle, (current, t) => current.CreateSubBundle(t));
                (bundle as Bundle).Content = value ?? ContentProcessor.GetEmptyContent();
            }
        }

        Bundle IBundleEventTracker.CreateBundle(Bundle parent, string name)
        {
            name = name.ToLower();
            var bundle = new Bundle(parent??(BundleCollectionBase)this,name);
            lock (m_UncommitedEvents)
            {
                m_UncommitedEvents.Add(new BundleData {Configuration = Name, Name = bundle.Name, Content = bundle.Content, Action = BundleAction.Create});
            }
            return bundle;
        }

        void IBundleEventTracker.UpdateBundle(Bundle bundle)
        {
            lock (m_UncommitedEvents)
            {
                m_UncommitedEvents.Add(new BundleData {Configuration = Name, Name = bundle.Name, Content = bundle.Content, Action = BundleAction.Save});
            }
        }

        void IBundleEventTracker.DeleteBundle(Bundle bundle)
        {
            lock (m_UncommitedEvents)
            {
                m_UncommitedEvents.Add(new BundleData {Configuration = Name, Name = bundle.Name, Content = bundle.Content, Action = BundleAction.Delete});
            }
        }


        public bool Commit()
        {
            try
            {
                lock (m_UncommitedEvents)
                {
                    m_Persister.Save(m_UncommitedEvents.ToArray());
                    var map = new SortedDictionary<string, string>();
                    Visit(b => map.Add(b.Name, b.Content));
                    Interlocked.Exchange(ref m_Map, map);
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
    }
}