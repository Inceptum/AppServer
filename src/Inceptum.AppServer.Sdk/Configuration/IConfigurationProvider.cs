namespace Inceptum.AppServer.Configuration
{
    public interface IConfigurationProvider
    {
        string GetBundle(string configuration, string bundleName, params string[] extraParams);
    }
}
