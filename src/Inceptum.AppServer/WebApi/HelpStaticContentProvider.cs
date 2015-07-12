using Inceptum.WebApi.Help;

namespace Inceptum.AppServer.WebApi
{
    internal class HelpStaticContentProvider : EmbeddedResourcesContentProvider
    {
        private readonly IContentProvider m_ParentContentProvider;

        public HelpStaticContentProvider(IContentProvider parentContentProvider)
            : base(typeof(HelpStaticContentProvider).Assembly, "Inceptum.AppServer.WebApi.Help.")
        {
            m_ParentContentProvider = parentContentProvider;
        }

        public override StaticContent GetContent(string resourcePath)
        {
            return base.GetContent(resourcePath) ?? m_ParentContentProvider.GetContent(resourcePath);
        }
    }
}