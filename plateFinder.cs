using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RSG;
using Newtonsoft.Json;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Text.Json;


namespace LPR_CSharp
{
    public class plateFinder
    {
        // get vehicle plate by plate number
        public IPromise<Dictionary<string, string>> findPlateByNumber(LprCamera lprCamera, string plateNumber)
        {
            var promise = new Promise<Dictionary<string, string>>();
            bool isFound = false;
            int searchCounter = 0;
            Dictionary<string, string> plate = new Dictionary<string, string>();
            while (!isFound && searchCounter < 1)
            {
                string bodyStr = "<? xml version =\\1.0\\ encoding=\\utf-8\\?>" +
                            "< config xmlns =\\http://www.ipc.com/ver10\\version=\\1.7\\>" +
                            "< vehiclePlates type =\\list\\ maxCount=\\10000\\ count=\\1000\\>" +
                            "< searchFilter >" +
                            "< carPlateNum type =\\string\\>" + plateNumber + "</carPlateNum>" +
                            "< listType type = unit32 > allList </ listType >" +
                            "</ searchFilter >" +
                            "</ vehiclePlates >" +
                            "</ config >";
                string apiStr = "/ GetVehiclePlate";
                Dictionary<string, string> requestData = new Dictionary<string, string>();
                requestData.Add("body", bodyStr);
                requestData.Add("api", apiStr);
                var httpLpr = new httpRequest();
                var response = httpLpr.makeRequest(lprCamera, requestData, " search plate: " + plateNumber + " in lpr camera: ");
                if (response != null)
                {
                    string jsonResponse;
                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(response.ToString());
                        jsonResponse = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                    }
                    catch (InvalidCastException e)
                    {
                        Console.WriteLine("Failed to convert xml of car number status in lpr to json, Error:" + e.StackTrace);
                        plate.Add("notResponse", "true");
                        promise.Resolve(plate);
                    }

                    try
                    {
                        XmlDocument doc = new XmlDocument();
                        doc.LoadXml(response.ToString());
                        jsonResponse = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                        //var jsonObj = JObject.Parse(jsonResponse);
                        //jsonResponse = jsonObj.ToString();
                        var jsonObj = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                        string count = jsonObj["config"]["vehiclePlates"]["_attributes"]["count"].ToString();
                        //string count = jsonResponse["config"]["vehiclePlates"]["_attributes"]["count"];
                        if (count == "1")
                        {
                            string carNumberStr = jsonObj["config"]["vehiclePlates"]["item"]["carPlateNumber"]["_cdata"].ToString();
                            string allowlistValueStr = jsonObj["config"]["vehiclePlates"]["item"]["plateItemType"]["_text"].ToString();
                            string keyIdStr = jsonObj["config"]["vehiclePlates"]["item"]["keyId"]["_text"].ToString();
                            plate.Add("carNumber", carNumberStr);
                            plate.Add("allowlistValue", allowlistValueStr);
                            plate.Add("keyId", keyIdStr);
                            break;
                        }
                        else if (count != "0" && count != "1")
                        {
                            var carData = jsonObj["carPlateNumber"]["_cdata"];
                            //var carData = jsonObj["config"]["vehiclePlates"]["item"];//.First((car) => jsonObj["carPlateNumber"]["_cdata"] == plateNumber);
                            foreach (var car in carData)
                            {
                                if (car.ToString() == plateNumber)
                                {
                                    string carNumberStr = jsonObj["config"]["vehiclePlates"]["item"]["carPlateNumber"]["_cdata"].ToString();
                                    string allowlistValueStr = jsonObj["config"]["vehiclePlates"]["item"]["plateItemType"]["_text"].ToString();
                                    string keyIdStr = jsonObj["config"]["vehiclePlates"]["item"]["keyId"]["_text"].ToString();
                                    plate.Add("carNumber", carNumberStr);
                                    plate.Add("allowlistValue", allowlistValueStr);
                                    plate.Add("keyId", keyIdStr);
                                }
                            }
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        Console.WriteLine("Failed to read data from json, Error:" + e.StackTrace);
                    }
                }
                searchCounter++;
            }
            promise.Resolve(plate);
            return promise;
        }

        //get all car in the lpr camer
        public IPromise<string> findAllPlateNumber(LprCamera lprCamera)
        {
            var promise = new Promise<string>();
            Dictionary<string, string> requestData = new Dictionary<string, string>();
            try
            {
                string bodyStr =
                "< config xmlns = http://www.ipc.com/ver10 version = 1.7 >" +
                    "< types >" +
                        "< vehicleListTypes >" +
                            "<enum> blackList</enum>" +
                            "<enum> whiteList</enum>" +
                            "<enum> strangerList</enum>" +
                            "<enum> allList</enum>" +
                        "</ vehicleListTypes >" +
                    "</ types >" +
                    "< vehiclePlates type = list maxCount = 10000 count = 1 >" +
                        "< searchFilter >" +
                            "< item >" +
                                "< pageIndex type = unit32 > 0 </ pageIndex >" +
                                "< pageSize type = unit32 > 10 </ pageSize >" +
                                "< listType type = vehicleListTypes > allList </ listType >" +
                                "< carPlateNum type = string ></ carPlateNum >" +
                            "</ item >" +
                        "</ searchFilter >" +
                    "</ vehiclePlates >" +
                "</ config >";
                string apiStr = "/ GetVehiclePlate";
                requestData.Add("body", bodyStr);
                requestData.Add("api", apiStr);
                var httpLpr = new httpRequest();
                var response = httpLpr.makeRequest(lprCamera, requestData, " get all plates in lpr camera:");
                string jsonResponse;
                if (response !=null)
                {
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(response.ToString());
                    jsonResponse = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
                    var jsonObj = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                }
                else
                    jsonResponse = null;
                promise.Resolve(jsonResponse);
            } catch (InvalidCastException e)
            {
                Console.WriteLine("error when try get all the car list in lpr camera: " + lprCamera.name + ", Error: " + e.StackTrace);
                promise.Reject(e);
            }
            return promise;
        }
    }
}
