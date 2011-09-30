namespace Inceptum.AppServer.Configuration
{
    public interface IContentProcessor
    {
        string Merge(string parentContent, string childContent);
        string Diff(string parentContent, string childContent);
        string GetEmptyContent();
    }
}