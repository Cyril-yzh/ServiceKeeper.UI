using ServiceKeeper.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceKeeper.Core.MediatR;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ServiceKeeper.UI.Utils
{

    public class LogLoader
    {
        private readonly string logPath = "";

        public LogLoader(ServiceKeeperUIOptions options)
        {
            logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"{options.LogDirName}");
        }
        public List<string>? LoadLog(string date)
        {
            string logFilePath = $"{logPath}/log-{date}.txt";
            if (!File.Exists(logFilePath))
            {
                return null;
            }

            // 读取日志文件中的内容
            var logEvents = new List<string>();

            using var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fileStream);
            {
                while (!reader.EndOfStream)
                {
                    string? logEvent = reader.ReadLine();
                    if (logEvent != null) { logEvents.Add(logEvent); }
                }
            }

            return logEvents;
        }
    }
}
