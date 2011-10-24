using Inceptum.AppServer.Management.Resources;

namespace Inceptum.AppServer.Management.Handlers
{
    public class ConfPageHandler
    {
        public ConfPage Get()
        {
            return new ConfPage {value = "hello world!!!"};
        }
    }
}
