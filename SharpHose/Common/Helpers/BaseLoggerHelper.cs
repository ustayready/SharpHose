using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace SharpHose.Common.Helpers
{
    public abstract class BaseLoggerHelper
    {
        protected bool Quiet;
        protected readonly object lockObj = new object();
        public abstract void Log(string message);
    }
    
    public class HTTPLogger : BaseLoggerHelper
    {
        public HttpWebRequest client;
        public string logUrl = string.Empty;

        public HTTPLogger(string url, string method = "POST")
        {
            var http = (HttpWebRequest)WebRequest.Create(new Uri(logUrl));
            http.Accept = "application/json";
            http.ContentType = "application/json";
            http.Method = method;
        }

        public override void Log(string message)
        {
            lock (lockObj)
            {
                try
                {
                    var dict = new Dictionary<string, string>();
                    dict.Add("time", String.Format("{0:u}", DateTime.Now));
                    dict.Add("log", message);

                    var entries = dict.Select(d => string.Format("\"{0}\": [{1}]", d.Key, string.Join(",", d.Value)));
                    var parsedContent = "{" + string.Join(",", entries) + "}";

                    var encoding = new ASCIIEncoding();
                    var bytes = encoding.GetBytes(parsedContent);

                    var newStream = client.GetRequestStream();
                    newStream.Write(bytes, 0, bytes.Length);
                    newStream.Close();

                    var response = client.GetResponse();
                    var stream = response.GetResponseStream();
                    var sr = new StreamReader(stream);
                    var content = sr.ReadToEnd();
                } catch { }
            }
        }
    }
    public class FileLogger : BaseLoggerHelper
    {
        public static string logDt = String.Format("{0:u}", DateTime.Now);
        public string logDir = string.Empty;
        public string logName = $"SharpHose_{logDt}.log";

        public FileLogger()
        {
            logDir = Path.GetTempPath();
        }

        public FileLogger(string path)
        {
            logDir = path;
        }

        public override void Log(string message)
        {
            lock (lockObj)
            {
                var dt = String.Format("{0:u}", DateTime.Now);
                var logPath = $"{logDir}{logName}";
                using (var streamWriter = new StreamWriter(logPath))
                {
                    streamWriter.WriteLine($"[{dt}] {message}");
                    streamWriter.Close();
                }
            }
        }
    }
    public class ConsoleLogger : BaseLoggerHelper
    {
        public ConsoleLogger(bool quiet)
        {
            Quiet = quiet;
        }
        public override void Log(string message)
        {
            if (!Quiet)
            {
                lock (lockObj)
                {
                    var dt = String.Format("{0:u}", DateTime.Now);
                    Console.WriteLine($"[{dt}] {message}");
                }
            }
        }
    }
}
