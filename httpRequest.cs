using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RSG;
using System.Text;
using System.Net;
using System.Text.Json;

namespace LPR_CSharp
{
    public class httpRequest
    {
        public IPromise<string> makeRequest(LprCamera lprCamera, Dictionary<string, string> requestData, string actionMessage)
        {
            var promise = new Promise<string>();
            string port = requestData["api"] == "/SetUnSubscribe" ? lprCamera.longPort : lprCamera.port;
            string url = "http://" + lprCamera.ip + ":" + port + requestData["api"];
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(lprCamera.userName + ":" + lprCamera.password);
            string basicAuth = "Basic " + System.Convert.ToBase64String(plainTextBytes);
            Dictionary<string, string> requestHeaders = new Dictionary<string, string>();
            requestHeaders.Add("requestHeaders", basicAuth);
            if (requestData["api"] == "/GetSnapshot") {
                requestHeaders["Accept"] = "application/binary";
            }

            if (requestData["api"] == "/DownloadImage") {
                requestHeaders["Accept"] = "application/x-ms-application, image/jpeg, application/xaml+xml, image/gif, image/pjpeg, application/x-ms-xbap, application/vnd.ms-excel, application/vnd.ms-powerpoint, application/msword, */*";
                requestHeaders["User-Agent"] = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; WOW64; Trident/4.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0; .NET4.0C; .NET4.0E; InfoPath.2)";
                requestHeaders["Content-Type"] = "application/x-www-form-urlencoded";
                requestHeaders["Accept-Encoding"] = "gzip, deflate";
                requestHeaders["Content-Length"] = requestData["body"].Length.ToString();
                requestHeaders["Connection"] = "Keep-Alive";
            }

            if (requestData["api"] == "/SetUnSubscribe") {
                requestHeaders["Accept"] = "*/*";
                requestHeaders["Content-Type"] = "text/plain";
                requestHeaders["Cache-Control"] = "no-cache";
                requestHeaders["Host"] = lprCamera.ip + ":" + port;
                requestHeaders["Accept-Encoding"] = "gzip, deflate, br";
                requestHeaders["Content-Length"] = requestData["body"].Length.ToString();
                requestHeaders["Connection"] = "keep-alive";
                requestHeaders["User-Agent"] = "PostmanRuntime/7.22.0";
                requestHeaders["Postman-Token"] = "5b9b1a61-94cc-451f-9a10-307eed9a7db9";
            }
            Console.WriteLine(actionMessage + lprCamera.name);
            string response;
            try {
                if (requestData["body"]!=null) {
                    //for get download image
                    if (requestData["api"] == "/DownloadImage") {
                        var request = WebRequest.Create(url);
                        request.Method = "POST";
                        var json = JsonSerializer.Serialize(requestData["body"]);
                        byte[] byteArray = Encoding.UTF8.GetBytes(json);
                        request.ContentType = "arraybuffer";
                        request.ContentLength = byteArray.Length;
                        using var reqStream = request.GetRequestStream();
                        reqStream.Write(byteArray, 0, byteArray.Length);
                        response = request.GetResponse().ToString();
                    } else {
                        //in all other requests
                        var request = WebRequest.Create(url);
                        request.Method = "POST";
                        var json = JsonSerializer.Serialize(requestData["body"]);
                        byte[] byteArray = Encoding.UTF8.GetBytes(json);
                        request.ContentLength = byteArray.Length;
                        using var reqStream = request.GetRequestStream();
                        reqStream.Write(byteArray, 0, byteArray.Length);
                        response = request.GetResponse().ToString();
                    }
                    promise.Resolve(response);
                }
                else
                {
                    if (requestData["api"] == "/GetSnapshot") {
                        var request = WebRequest.Create(url);
                        request.Method = "GET";
                        var json = JsonSerializer.Serialize(requestData["body"]);
                        byte[] byteArray = Encoding.UTF8.GetBytes(json);
                        request.ContentType = "arraybuffer";
                        request.ContentLength = byteArray.Length;
                        using var reqStream = request.GetRequestStream();
                        reqStream.Write(byteArray, 0, byteArray.Length);
                        response = request.GetResponse().ToString();                       
                    } else {
                        var request = WebRequest.Create(url);
                        request.Method = "GET";
                        var json = JsonSerializer.Serialize(requestData["body"]);
                        byte[] byteArray = Encoding.UTF8.GetBytes(json);
                        request.ContentLength = byteArray.Length;
                        using var reqStream = request.GetRequestStream();
                        reqStream.Write(byteArray, 0, byteArray.Length);
                        response = request.GetResponse().ToString();
                    }
                    promise.Resolve(response);
                }
            } catch (InvalidCastException e) {
                Console.WriteLine("$makeRequest error - " + e.StackTrace);
                promise.Resolve("not response from lpr camera");
            }
            return promise;
        }
    }
}
