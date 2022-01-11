using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Xml;
using Newtonsoft.Json.Linq;
using RSG;

namespace LPR_CSharp
{
    public class lprevent
    {
        string nl = "\r\n'";
        public void handleNewData(string data, LprCamera lprCamera)
        {
            lprCamera.streamingData += data;

            if (!lprCamera.builtUnsubscribeConfig &&
                lprCamera.streamingData.Contains("<serverAddress") &&
                lprCamera.streamingData.Contains("</serverAddress"))
                new builtUnsubscribeConfig(lprCamera);

            var indexOfClosingTag = lprCamera.streamingData.IndexOf("</config>");

            if (indexOfClosingTag != -1)
            {
                indexOfClosingTag += "</config>".Length;
                lprCamera.dataToCheck = lprCamera.streamingData.Substring(0, indexOfClosingTag);
                lprCamera.streamingData = lprCamera.streamingData.Substring(indexOfClosingTag);
                checkStreamData(lprCamera);
            }
        }

        public string Slice(string source, int start, int end)
        {
            if (end < 0)
            {
                end = source.Length + end;
            }
            int len = end - start;         
            return source.Substring(start, len);
        }

        public async void checkStreamData(LprCamera lprCamera)
        {
            try
            {
                var start = lprCamera.dataToCheck.IndexOf("<config");
                var end = lprCamera.dataToCheck.IndexOf("</config>");

                if (start != -1 && end != -1)
                {
                    var xmlData = Slice(lprCamera.dataToCheck, start, end + 10);
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xmlData);
                    string jsonResponse;
                    try
                    {
                        jsonResponse = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);                        
                    }
                    catch (InvalidCastException e)
                    {
                        Console.WriteLine("Error when try converting event xml data to json from Lpr camera:" + lprCamera.name + ", Error: " + e.StackTrace);
                    }
                    jsonResponse = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                    var jsonObj = JObject.Parse(jsonResponse);

                    if (isSetRenewResponse(jsonObj, lprCamera))
                    {
                        if (!lprCamera.isAlive)
                            lprCamera.updateSuccessConntion();
                    }

                    if (jsonObj!=null && jsonObj["config"]["plateCount"]!=null && jsonObj["config"]["plateCount"]["_text"].ToString() == "1")
                    {
                        string eventName = jsonObj["config"]["smartType"]["_text"].ToString();
                        //update that we get new message
                        newEvent(lprCamera, eventName, end);
                    }
                    else
                    {
                        cleanStreamData(lprCamera, end);
                    }
                }
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void  cleanStreamData(LprCamera lprCamera, int end)
        {
            lprCamera.streamingData= lprCamera.streamingData.Substring(end + 10, lprCamera.streamingData.Length);
        }

        public bool isSetRenewResponse(JObject data, LprCamera lprCamera)
        {
            var x = data.Count;
            if (x != 1) return false;
            var y = 0;
            foreach (JProperty property in data.Properties())
            {
                if (property.Value == null) return false;
                y++;
            }
            if (y != 3) return false;

            Console.WriteLine(DateTime.Now.ToString() + "-response to setRenew");
            lprCamera.resetSetRenewTimeout();
            return true;            
        }


        public async void newEvent(LprCamera lprCamera, string eventName, int end)
        {
            //check the event type
            if(eventName == "VEHICE")
            {
                List<Dictionary<string, string>> tevents = await getTheCarDetection(lprCamera);
                foreach(var events in tevents)
                {
                    Console.WriteLine(" ***** New event *****");
                    Console.WriteLine("Car number - " + events["carNumber"]);
                    Console.WriteLine("Date - " + events["eventTime"]);
                    Console.WriteLine("Arrived in time - " + events["eventAcceptedInTime"]);
                    if (events["eventType"].ToString() == "1")
                        Console.WriteLine("Allowed car - this car in white list");
                    else if (events["eventType"].ToString() == "0")
                        Console.WriteLine("Not allowed car - this car in black list");
                    else if (events["eventType"].ToString() == "2")
                        Console.WriteLine("Not allowed car - this Unknown car");
                    Console.WriteLine(" ***** " + lprCamera.name + " ***** ");
                }
            }
            else if(eventName == "MOTION")
            {

            }
            else if(eventName == "videoloss")
            {

            }
            else
            {
                Console.WriteLine("******************************");
                Console.WriteLine("Get new event of:" + eventName + ", from LPR camera: " + lprCamera.name);
                Console.WriteLine("******************************");
            }
        }


        public async Task<List<Dictionary<string, string>>> getTheCarDetection(LprCamera lprCamera)
        {
            Dictionary<string, string> events = new Dictionary<string, string>();
            List<Dictionary<string, string>> tevents = new List<Dictionary<string, string>>();
            try
            {
                string eventXml = lprCamera.dataToCheck;
                string[] evXmls = eventXml.Split(nl + nl);

                for (int i = 0; i < evXmls.Length; i++)
                {
                    string xmlEvent = evXmls[i];

                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(xmlEvent);
                        string jsonResponse = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                        var jEvent = JObject.Parse(jsonResponse);
                        List<JToken> items = jEvent["config"]["listInfo"]["item"].ToList();
                        events.Add("lprCameraName", lprCamera.name);
                        events.Add("carNumber", items[1]["plateNumber"]["_cdata"].ToString());
                        events.Add("eventImage", "data: image / jpeg; base64," + items[1]["targetImageData"]["targetBase64Data"]["_cdata"].ToString());
                        events.Add("data", "image / jpeg; base64," + items[0]["targetImageData"]["targetBase64Data"]["_cdata"].ToString());
                        events.Add("vehicleDirection", items[1]["vehicleDirect"]!=null ? items[1]["vehicleDirect"]["_text"].ToString(): null);
                        string milliseconds = jEvent["config"]["currentTime"]["_text"].ToString();
                        int newlength = milliseconds.Length - 3;
                        milliseconds = milliseconds.Substring(0, newlength);
                        events.Add("eventTime", new DateTime(Int32.Parse(milliseconds)).ToString("yyyyMMddTHH:mm:ssZ"));
                        plateFinder ptFind = new plateFinder();
                        var plate = ptFind.findPlateByNumber(lprCamera, events["carNumber"]);
                        if (plate!=null)
                        {
                            events.Add("eventType", "2");
                        }
                        else
                        {
                            //events.Add("eventType", plate["allowlistValue"] == "blackList" ? "0" : plate["allowlistValue"] == "whiteList" ? "1" : plate["notResponse"] == "true" ? "3" : "2");
                            events.Add("eventType", "1");
                        }

                        events.Add("eventAcceptedInTime", DateTime.Now.ToString("yyyyMMddTHH:mm:ssZ"));
                    }
                    catch (InvalidCastException e)
                    {
                        Console.WriteLine("Error in LPR camera :" + lprCamera.name + ", cannot get the last events," + e.StackTrace);
                        return null;
                    }
                    tevents.Add(events);
                }
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine("Error in LPR camera :" + lprCamera.name + ", cannot get the last events," + e.StackTrace);
                return null;
            }
            return tevents;
        }
    }
}
