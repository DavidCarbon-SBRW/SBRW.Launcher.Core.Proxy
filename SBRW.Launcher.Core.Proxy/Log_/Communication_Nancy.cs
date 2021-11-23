using Newtonsoft.Json;
using SBRW.Launcher.Core.Extension.Logging_;
using SBRW.Launcher.Core.Extension.Time_;
using System;
using System.Collections.Generic;
using System.IO;

namespace SBRW.Launcher.Core.Proxy.Log_
{
    internal enum CommunicationLogEntryType
    {
        Request,
        Response,
        Info,
        Error,
        Rejected,
        Warning,
        Timer,
        Unknown
    }

    internal interface ICommunicationLogData
    {
        string Path { get; set; }
        string Method { get; set; }
    }

    internal class CommunicationLogEntry
    {
        public string RecordedAt { get; set; }
        public string ServerId { get; set; }
        public string Category { get; set; }
        public string Type { get; set; }
        public ICommunicationLogData Data { get; set; }
    }

    internal class CommunicationLogRequest : ICommunicationLogData
    {
        public string Body { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }

        public CommunicationLogRequest(string body, string path, string method)
        {
            Body = body;
            Path = path;
            Method = method;
        }
    }

    internal class CommunicationLogResponse : ICommunicationLogData
    {
        public string Body { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }

        public CommunicationLogResponse(string body, string path, string method)
        {
            Body = body;
            Path = path;
            Method = method;
        }
    }

    internal class CommunicationLogLauncherError : ICommunicationLogData
    {
        public string Message { get; set; }
        public string Path { get; set; }
        public string Method { get; set; }

        public CommunicationLogLauncherError(string message, string path, string method)
        {
            Message = message;
            Path = path;
            Method = method;
        }
    }

    internal static class Communication_Nancy
    {
        public static void RecordEntry(string ID, string CAT, CommunicationLogEntryType TYPE, ICommunicationLogData DATA)
        {
            try
            {
                if (!Directory.Exists(Log_Location.LogCurrentFolder))
                {
                    Directory.CreateDirectory(Log_Location.LogCurrentFolder);
                }
            }
            catch { }

            try
            {
                CommunicationLogEntry Entry = new CommunicationLogEntry
                {
                    ServerId = ID,
                    Category = CAT,
                    Data = DATA,
                    Type = CallMethod(TYPE),
                    RecordedAt = Time_Clock.GetTime("Now - UTC Time (Offset)")
                };

                File.AppendAllLines(Log_Location.LogCommunication, new List<string>
                {
                    JsonConvert.SerializeObject(Entry, Formatting.Indented)
                });
            }
            catch (Exception Error)
            {
                Log_Detail.OpenLog("Communication", null, Error, null, true);
            }
        }

        private static string CallMethod(CommunicationLogEntryType Type)
        {
            switch (Type)
            {
                case CommunicationLogEntryType.Error:
                    return "ERROR";
                case CommunicationLogEntryType.Info:
                    return "INFO";
                case CommunicationLogEntryType.Request:
                    return "REQUEST";
                case CommunicationLogEntryType.Response:
                    return "RESPONSE";
                case CommunicationLogEntryType.Rejected:
                    return "REJECTED";
                case CommunicationLogEntryType.Warning:
                    return "WARNING";
                case CommunicationLogEntryType.Timer:
                    return "TIMER";
                default:
                    return "UNKNOWN";
            }
        }
    }
}
