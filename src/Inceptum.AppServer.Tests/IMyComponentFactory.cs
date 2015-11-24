namespace Inceptum.AppServer.Tests
{
    public interface IMyComponentFactory
    {
        MyComponent Create(string name);
        void Release(MyComponent component);
    }
}