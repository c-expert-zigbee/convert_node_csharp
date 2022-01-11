using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Timers;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNet.SignalR.Configuration;
using Microsoft.AspNet.SignalR.Hosting;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.AspNet.SignalR.Json;
using Microsoft.AspNet.SignalR.Tracing;
using Newtonsoft.Json;
using System.Xml;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.ServiceProcess;

namespace LPR_CSharp
{
    public class LprCamera
    {
        public string name = "";
        public string ip = "";
        public string port = "";
        public string longPort = "";
        public string userName = "";
        public string password = "";
        public string numOfTry = "";
        public bool builtUnsubscribeConfig = false;
        public string unsubscribeData = "";
        public string streamingData = "";
        public string dataToCheck = "";
        public bool isAlive = false;

        int connectionInterval;
        int tries;  
        bool connection;
        public TcpClient tcpclnt;
        bool keepAlive = true;
        bool setRenewTimeout = false;
        string longPollingPort = null;
        string basicAuth = "";
         
        string setRenewInterval = null;
        string nl = "\r\n";
        NetworkCredential networkCredential;
        public LprCamera(Dictionary<string, string> lprCamera)
        {
            this.setProperties(lprCamera);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(lprCamera["userName"] + ":" + lprCamera["password"]);
            this.basicAuth = "Basic " + System.Convert.ToBase64String(plainTextBytes);
            tries = 1;
            connectionInterval = 1;
            connection = false;
            networkCredential = new NetworkCredential(userName, password);
            handler = new HttpClientHandler() { Credentials = networkCredential };
            _httpClient = new HttpClient(handler);

        }
        public void setProperties(Dictionary<string, string> lprCamera)
        {
            this.name = lprCamera["name"];
            this.ip = lprCamera["ip"];
            this.port = lprCamera["port"];
            this.userName = lprCamera["userName"];
            this.password = lprCamera["password"];
        }

        public void connect()
        {
            this.keepAlive = true;
            try
            {
                tryToConnectInterval();
            }
            catch (InvalidCastException e)
            {
                Console.WriteLine(this.name + " - " + e.StackTrace);
                this.closeConnection();
            }
        }

        public void SetTimer()
        {
            // Create a timer with a two second interval.
            System.Timers.Timer timer = new System.Timers.Timer();
            // Hook up the Elapsed event for the timer.
            timer.Interval = 10000;
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.Start();
            Console.ReadLine();
        }

        private static void WriteLogEntry(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer is active : " + e.SignalTime);
        }

        public void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            startConnection(tries++);
        }

        public void tryToConnectInterval()
        {
            if (this.connectionInterval == 0)
                return;
            SetTimer();
            setRenewTimeout = true;
        }

        public void startConnection(int tries)
        {
            closeConnectionIfExists();
            try
            {
                tcpclnt = new TcpClient();
                Console.WriteLine(this.name + " - Connecting(" + tries + ")");
                IPAddress ipAd = IPAddress.Parse(ip);
                tryStartingConnection();
                tcpclnt.Connect(ipAd, int.Parse(port));                
                connection = true;
            }
            catch (Exception e) {
                Console.WriteLine(" - Error " + e.StackTrace);
            }
        }

        public async void tryStartingConnection()
        {
            try
            {
                defineSocketListeners();
                getAndSetLongPollingPort();
                connectionInterval = 0;
                subscribeToEvents();
                Console.WriteLine(name + " - " + DateTime.Now.ToString() + "-Close");
                updateStatus("disconnected");
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }
        }

        public void subscribeToEvents()
        {
            XmlTextReader subscribeConfig = new XmlTextReader("./utils/provisionLprCameraSubscribeConfig.xml");
            string header = "POST / SetSubscribe HTTP / 1.1" + nl +
            "Host: " + ip + ":" + longPort + nl +
            "Authorization: " + basicAuth + nl +
            "Connection: Keep - Alive" + nl +
            "Content - Type: application / x - www - form - urlencoded" + nl +
            "Content - Length: " + subscribeConfig.ToString().Length.ToString() + nl + nl;
            string subscribeConfigStr = header + subscribeConfig.ToString();
            var response = subscribeConfig;
            Console.WriteLine("you need to check for response of subscribe!");
        }

        public void defineSocketListeners()
        {
            lprevent lpEv = new lprevent();
            lpEv.handleNewData(streamingData, this);
            Console.WriteLine(name + " - " + DateTime.Now.ToString() + "-isteners defined");
        }
        public void resetConneciton()
        {
            this.closeConnection();
            this.connect();
        }

        public void closeConnection()
        {            
            tcpclnt.Close();
            connection = false;
        }

        public void destroy()
        {
            this.keepAlive = false;
            Console.WriteLine("destroy");
            this.stopConnectionInterval();
            this.closeConnection();
        }

        public void updateSuccessConntion()
        {
            Console.WriteLine(this.name + " - " + DateTime.Now.ToString());
            updateStatus("connected");

            this.isAlive = true;
        }

        public async void getAndSetLongPollingPort()
        {
            string response = await this.callAPI("GetPortConfig");

            if (response!=null)
            {
                setLongPollingPort(response);
            }
            else
            {
                Console.WriteLine("Cannot get longPolling port from camera: " + this.name);
            }
        }
        HttpClientHandler handler;
        private  HttpClient _httpClient = new HttpClient();
        public async Task<string> callAPI(string apiPath)
        {
           
            var url = "http://" + this.ip + ":" + this.port + "/" + apiPath;
            var html = await _httpClient.GetStringAsync(url);
            return html.ToString();
            
        }

        public void SetTimer1()
        {
            System.Timers.Timer timer = new System.Timers.Timer();
            // Hook up the Elapsed event for the timer.
            timer.Interval = 5000;
            timer.AutoReset = true;
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent1);
            timer.Start();
            Console.ReadLine();
        }

        public void OnTimedEvent1(Object source, ElapsedEventArgs e)
        {
            setRenewEvent();
        }

        public void startSetRenewInterval()
        {            
            try
            {
                SetTimer1();
                setRenewTimeout = true;
            }
            catch (Exception e)
            {
                Console.WriteLine(" - Error " + e.StackTrace);
            }
        }
        public void stopSetRenewInterval()
        {
            if (setRenewInterval!=null)
            {
                setRenewInterval = null;
            }
        }

        public void stopConnectionInterval()
        {
            if (connectionInterval>0)
            {
                this.connectionInterval = 0;
            }
        }

        public void resetSetRenewTimeout()
        {
            if (this.setRenewTimeout)
            {
                this.setRenewTimeout = false;
            }
            Thread.Sleep(40000);
            resetConneciton();
        }

        public void stopSetRenewTimeOut()
        {
            if (this.setRenewTimeout)
            {
                setRenewTimeout = false;
            }
        }

        public void setRenewEvent()
        {
            if (unsubscribeData == null)
            {
                unsubscribeData = "< ![CDATA[http://" + ip + ":" + longPort + "/TVT/event/subsription_" + numOfTry.ToString() + "]}]]>";
            }
            string xml = "<? xml version = 1.0 encoding = UTF-8 ?>" + nl.ToString() +
                "< config version = 1.0 xmlns = http://www.ipc.com/ver10 >" + nl.ToString() +
                "< serverAddress type = string >" + unsubscribeData + "]}</ serverAddress >" + nl.ToString() +
                "< renewTime type = uint32 > 60 </ renewTime >" + nl.ToString() +
                "</ config >" + nl.ToString();

            string header = "POST / SetRenew HTTP / 1.1" + nl.ToString() +
                "Host: https://" + ip +":" + longPort.ToString() + nl.ToString() +
                "Authorization:" + basicAuth + nl.ToString() +
                "Content - Type: application / xml; charset = utf - 8" + nl.ToString() +
                "Content - Length: " + xml.Length.ToString() + nl.ToString() + nl.ToString();

            string setRenew = header + xml;
            string response = setRenew;
            if (response!=null)
            {
                Console.WriteLine(DateTime.Now.ToString() + "-Set Renew sent");
            }
            else
            {
                Console.WriteLine(DateTime.Now.ToString() + "-Set Renew Fail");
            }                
        }

        public void setLongPollingPort(string data)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(data);
            string jsonResponse = JsonConvert.SerializeXmlNode(doc, Newtonsoft.Json.Formatting.Indented);
            dynamic jsonObj = JObject.Parse(jsonResponse);
            var config = jsonObj.config;
            var port=jsonObj.ToString();
            //var 
            
            longPollingPort = jsonObj["config"]["port"]["longPollingPort"]["#text"].ToString();
        }

        public void updateStatus(string newStatus)
        {
            Console.WriteLine(this.name + " - " + newStatus);
        }

        public void closeConnectionIfExists()
        {
            if (this.connection)
                closeConnection();
        }
    }

   
}
