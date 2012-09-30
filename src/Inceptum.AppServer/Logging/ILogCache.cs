namespace Inceptum.AppServer.Logging
{
    public interface ILogCache
    {
        void Add(LogEvent message);
    }
}