using System;
using System.Linq;

namespace Inceptum.AppServer.Configuration.Model
{
    public class Bundle : BundleCollectionBase
    {
        internal Bundle(IContentProcessor contentProcessor, IBundleEventTracker eventTracker, string name, string content = null)
            : base(contentProcessor, eventTracker,  name)
        {
            if (eventTracker == null) throw new ArgumentNullException("eventTracker");
          
            m_PureContent = content ?? contentProcessor.GetEmptyContent();
        }


        internal Bundle(BundleCollectionBase parent, string name)
            : base(parent.ContentProcessor, parent.EventTracker,name)
        {
            if (parent == null) throw new ArgumentNullException("parent");
            Parent = parent as Bundle;
            m_PureContent=ContentProcessor.GetEmptyContent();
        }

        public Bundle Parent { get; private set; }

        private string m_PureContent;
        public string PureContent
        {
            get { return m_PureContent; }
            set
            {
                m_PureContent = value ?? ContentProcessor.GetEmptyContent();
                EventTracker.UpdateBundle(this);
            }
        }

        public override string Name
        {
            get
            {
                return Parent != null
                        ? Parent.Name+'.'+base.Name
                        : base.Name;
            }
        }


        public string ShortName
        {
            get
            {
                return base.Name;
            }
        }

        public string Content
        {
            get
            {
                return Parent != null
                           ? ContentProcessor.Merge(Parent.Content ?? ContentProcessor.GetEmptyContent(), PureContent ?? ContentProcessor.GetEmptyContent())
                           : PureContent;
            }
           
        }

        public bool IsEmpty
        {
            get { return ContentProcessor.IsEmptyContent(PureContent); }
        }
 

        public void Clear()
        {
            foreach (var child in this.ToArray())
            {
                child.Clear();
                EventTracker.DeleteBundle(child);
            }
        }
    }
}
