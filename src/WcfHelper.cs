using System;
using System.ServiceModel;

namespace Inceptum.AppServer.Utils
{
    public static class WcfHelper
    {
         public static  NetNamedPipeBinding CreateUnlimitedQuotaNamedPipeLineBinding()
        {
            return new NetNamedPipeBinding
            {
                ReceiveTimeout = TimeSpan.MaxValue,
                SendTimeout = TimeSpan.MaxValue,
                MaxReceivedMessageSize = int.MaxValue,
                MaxBufferSize = int.MaxValue,
                ReaderQuotas =
                {
                    MaxArrayLength = int.MaxValue,
                    MaxStringContentLength = int.MaxValue,
                    MaxBytesPerRead = int.MaxValue,
                    MaxNameTableCharCount = int.MaxValue,
                    MaxDepth = 256
                }
            };
        }
         
    }
}