namespace Inceptum.AppServer.Configuration
{
    public interface IContentProcessor
    {
        bool IsEmptyContent(string content);
        string Merge(string parentContent, string childContent);
        string Diff(string parentContent, string childContent);
        string GetEmptyContent();
    }
}
