using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LPR_CSharp
{
    public class builtUnsubscribeConfig
    {
       public builtUnsubscribeConfig(LprCamera lprCamera)
        {
            var indexOfOpenTagStreamingData = lprCamera.streamingData.IndexOf("<serverAddress");
            var indexOfCloseTagStreamingData = lprCamera.streamingData.IndexOf("</serverAddress");
            lprCamera.unsubscribeData = lprCamera.streamingData.Substring(indexOfOpenTagStreamingData + 30, indexOfCloseTagStreamingData);
            var indexOfNumOfTry = lprCamera.streamingData.IndexOf("_");
            lprCamera.numOfTry = lprCamera.streamingData.Substring(indexOfNumOfTry + 1, indexOfCloseTagStreamingData - 3);
            lprCamera.builtUnsubscribeConfig = true;
            lprCamera.startSetRenewInterval();
        }
    }
}
