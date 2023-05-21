using Newtonsoft.Json;
using SBRW.Launcher.Core.Extension.Logging_;
using SBRW.Launcher.Core.Extension.Time_;
using SBRW.Launcher.Core.Proxy.Nancy_;
using System;
using System.Collections.Generic;
using System.IO;

namespace SBRW.Launcher.Core.Proxy.Log_
{
    /// <summary>
    /// Log File Save Entry Type
    /// </summary>
    public enum CommunicationLogRecord
    {
        /// <summary>
        /// Saves All Recorded Entry Types
        /// </summary>
        All = 1,
        /// <summary>
        /// Saves Errors Entry Types
        /// </summary>
        Errors = 2,
        /// <summary>
        /// Saves Requests Entry Types
        /// </summary>
        Requests = 4,
        /// <summary>
        /// Saves Responses Entry Types
        /// </summary>
        Responses = 3,
        /// <summary>
        /// Saves No Entry Types
        /// </summary>
        None = 0
    }

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
            if(!Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.None))
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
                        RecordedAt = Time_Clock.GetTime(4)
                    };

                    File.AppendAllLines(Log_Location.LogCommunication, new List<string>
                    {
                        JsonConvert.SerializeObject(Entry, Formatting.Indented)
                    });
                }
                catch (Exception Error)
                {
                    Log_Detail.Full("Communication", Error);
                }
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
