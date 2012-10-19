using System;

namespace Inceptum.AppServer.Configuration.Model
{
    public class Bundle : BundleCollectionBase
    {
      
        private readonly IBundleObserver m_Observer;

        internal Bundle(IContentProcessor contentProcessor, IBundleObserver observer, string name, string content = null)
            : base(contentProcessor, name)
        {
            if (observer == null) throw new ArgumentNullException("observer");
            m_Observer = observer;
            PureContent = content ?? contentProcessor.GetEmptyContent();
        }


        private Bundle(IContentProcessor contentProcessor,IBundleObserver observer,Bundle parent,  string name)
            : this(contentProcessor, observer, name, null)
        {
            Parent = parent;
        }

        public Bundle Parent { get; private set; }

        public string PureContent { get; private set; }

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
                           ? ContentProcessor.Merge(Parent.Content, PureContent)
                           : PureContent;
            }
            set
            {
              /*  PureContent=Parent != null
                              ? ContentProcessor.Diff(Parent.Content, value)
                              : value;*/
                PureContent = value;

            }
        }

        public bool IsEmpty
        {
            get { return ContentProcessor.IsEmptyContent(PureContent); }
        }

        protected override Bundle CreateSubBundle(string name)
        {
            var bundle = new Bundle(ContentProcessor, m_Observer, this, name);
            m_Observer.OnChildCreated(this,bundle);
            return bundle;
        }

        public void Clear()
        {
            foreach (var child in this)
            {
                child.Clear();
            }
            Content = ContentProcessor.GetEmptyContent();
        }
    }
}
