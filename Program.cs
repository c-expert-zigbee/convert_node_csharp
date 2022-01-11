using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LPR_CSharp
{
    public class Program
    {
        
        public static void Main(string[] args)
        {
            //CreateHostBuilder(args).Build().Run();
            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("name", "cam1");
            data.Add("ip", "84.95.205.17");
            data.Add("port", "20000");
            data.Add("userName", "admin");
            data.Add("password", "!2#4QwEr");
            var lprCamera = new LprCamera(data);
            lprCamera.connect();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
