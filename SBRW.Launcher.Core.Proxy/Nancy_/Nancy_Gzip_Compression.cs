using SBRW.Nancy;
using SBRW.Nancy.Bootstrapper;
using SBRW.Nancy.Responses;
using SBRW.Launcher.Core.Cache;
using SBRW.Launcher.Core.Extension.Logging_;
using SBRW.Launcher.Core.Required.Anti_Cheat;
using SBRW.Launcher.Core.Proxy.Log_;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SBRW.Launcher.Core.Recommended.Time_;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    /// <summary>
    /// Log File Save Entry Type
    /// </summary>
    public enum GzipVersion
    {
        /// <summary>
        /// 2.3.X Revision of GZIP Handler
        /// </summary>
        Four = 4,
        /// <summary>
        /// 2.1.8.X Revision of GZIP Handler
        /// </summary>
        Three = 0,
        /// <summary>
        /// 2.1.7.2 Revision of GZIP Handler
        /// </summary>
        Two = 2,
        /// <summary>
        /// 2.1.6.9 Slight Revision of GZIP Handler
        /// </summary>
        OneV2 = 3,
        /// <summary>
        /// 2.1.6.X Revision of GZIP Handler
        /// </summary>
        One = 1,
    }
    /// <summary>
    /// 
    /// </summary>
    public class Nancy_Gzip_Compression : IApplicationStartup
    {
        private static bool Stop_Lock { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data_Pipelines"></param>
        public void Initialize(IPipelines Data_Pipelines)
        {
            Data_Pipelines.AfterRequest += CheckForCompression;
            Data_Pipelines.OnError += OnError;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="Error"></param>
        /// <returns></returns>
        private TextResponse OnError(NancyContext context, Exception Error)
        {
            Log.Error("PROXY HANDLER: " + context.Request.Path);
            Log_Detail.Full("PROXY HANDLER", Error);

            if (Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.Errors) || Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.All))
            {
                Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "PROXY", CommunicationLogEntryType.Error,
                new CommunicationLogLauncherError(Error.Message, context.Request.Path, context.Request.Method));
            }

            context.Request.Dispose();

            return new TextResponse(!Proxy_Settings.Ignore_Errors ? HttpStatusCode.BadRequest : HttpStatusCode.OK, Error.Message);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Reason"></param>
        /// <param name="Context"></param>
        private static void WebCallRejected(string Reason, NancyContext Context)
        {
            string ErrorReason = "[Launcher to Game Client] Web Call Rejected. ";
            switch (Reason)
            {
                case "RequestIsGzipCompatible":
                    ErrorReason += "Request Is Not Gzip Compatible";
                    break;
                case "ResponseIsCompatibleMimeType":
                    ErrorReason += "Response Is Not a Compatible Mime-Type";
                    break;
                case "ResponseIsCompressed":
                    ErrorReason += "Response Is Already Compressed";
                    break;
                case "ContentLengthIsTooSmall":
                    ErrorReason += "Content-Length Is Too Small";
                    break;
                case "GameTimer":
                    ErrorReason += "Game Requires Termination";
                    break;
                default:
                    ErrorReason += "Unknown Reason";
                    break;
            }

            if (Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.Requests) || Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.All))
            {
                Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "LAUNCHER", CommunicationLogEntryType.Rejected,
                new CommunicationLogLauncherError(ErrorReason, Context.Request.Path, Context.Request.Method));
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Context"></param>
        private static void CheckForCompression(NancyContext Context)
        {
            try
            {
                if (!RequestIsGzipCompatible(Context))
                {
                    WebCallRejected("RequestIsGzipCompatible", Context);
                }
                else if (ResponseIsCompressed(Context))
                {
                    WebCallRejected("ResponseIsCompressed", Context);
                }
                else if (!ResponseIsCompatibleMimeType(Context))
                {
                    WebCallRejected("ResponseIsCompatibleMimeType", Context);
                }
                else if (ContentLengthIsTooSmall(Context))
                {
                    WebCallRejected("ContentLengthIsTooSmall", Context);
                }
                else
                {
                    CompressResponse(Context);
                }
            }
            finally
            {
                if (!Launcher_Value.Game_In_Event && (Time_Window.Session_Expired || (Time_Window.Timer_Dynamic && Session_Timer.Remaining <= 0)) && !Stop_Lock)
                {
                    try
                    {
                        Stop_Lock = Launcher_Value.Game_In_Event_Bug = true;
                        Process[] Its_The_Law = Process.GetProcessesByName("nfsw");

                        try
                        {
                            if (Its_The_Law != null)
                            {
                                if (Its_The_Law.Length > 0)
                                {
                                    foreach (Process Law_ID in Its_The_Law)
                                    {
                                        if (!Process.GetProcessById(Law_ID.Id).HasExited)
                                        {
                                            if (!Process.GetProcessById(Law_ID.Id).CloseMainWindow())
                                            {
                                                Process.GetProcessById(Law_ID.Id).Kill();
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception Error)
                        {
                            Log_Detail.Full("Close Request (Proxy)", Error);
                        }

                        Stop_Lock = false;
                    }
                    catch (Exception Error)
                    {
                        Log_Detail.Full("Process Request (Proxy)", Error);
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Context"></param>
        /// <remarks>Based on Different <see href="https://gist.github.com/DavidCarbon/e0b37e7bc58b5e1a46f6dfedc87c966d">Solutions</see> 
        /// With Different OoM Errors</remarks>
        private static void CompressResponse(NancyContext Context)
        {
            if (Proxy_Settings.Gzip_Version == GzipVersion.One || Proxy_Settings.Gzip_Version == GzipVersion.OneV2)
            {
                Context.Response.Headers["Content-Encoding"] = "gzip";
                Context.Response.Headers["Connection"] = "close";

                /* Ask System to Allocate Memory */
                var Modded_Content = new MemoryStream();
                /* Response Contents is now feed into Allocated Memory */
                Context.Response.Contents(Modded_Content);
                /* Set Position for data in Allocated Memory */
                Modded_Content.Position = 0;
                /* Read the Contents from Allocated Memory */
                Context.Response.Contents = Response_Stream =>
                {
                    using (var gzip = new GZipStream(Response_Stream, CompressionMode.Compress, Proxy_Settings.Gzip_Version == GzipVersion.One))
                    {
                        /* Instead of Feeding content Raw (Which can potentially cause OoM) Lets read it from Allocated Memory */
                        gzip.Write(Modded_Content.ToArray(), 0, (int)Modded_Content.Length);
                    }
                };
            }
            else if (Proxy_Settings.Gzip_Version == GzipVersion.Two)
            {
                Context.Response.Headers["Content-Encoding"] = "gzip";
                Context.Response.Headers["Connection"] = "close";

                var Modded_Content = Context.Response.Contents;
                Context.Response.Contents = responseStream =>
                {
                    using (var compression = new GZipStream(responseStream, CompressionMode.Compress))
                    {
                        Modded_Content(compression);
                    }
                };
            }
            else
            {
                bool Deflate = Context.Request.Headers.AcceptEncoding.Any(x => x.Contains("deflate"));

                Context.Response.Headers["Content-Encoding"] = Deflate ? "deflate" : "gzip";
                Context.Response.Headers["Connection"] = "close";

                var Modded_Content = Context.Response.Contents;

                Context.Response.Contents = Response_Stream =>
                {
                    if (Proxy_Settings.Gzip_Version == GzipVersion.Four)
                    {
                        using (MemoryStream Memory_Stream = new MemoryStream())
                        {
                            if (Deflate)
                            {
                                using (DeflateStream Compressed = new DeflateStream(Memory_Stream, CompressionLevel.Optimal, true))
                                {
                                    Modded_Content(Compressed);
                                }
                            }
                            else
                            {
                                using (GZipStream Compress = new GZipStream(Memory_Stream, CompressionMode.Compress, true))
                                {
                                    Modded_Content(Compress);
                                }
                            }

                            // Set the correct Content-Length after compression
                            Memory_Stream.Position = 0;
                            Context.Response.Headers["Content-Length"] = Memory_Stream.Length.ToString();
                            Memory_Stream.CopyTo(Response_Stream);
                        }
                    }
                    else
                    {
                        if (Deflate)
                        {
                            using (DeflateStream Compressed = new DeflateStream(Response_Stream, CompressionLevel.Optimal, true))
                            {
                                Modded_Content(Compressed);
                            }
                        }
                        else
                        {
                            using (GZipStream Compress = new GZipStream(Response_Stream, CompressionMode.Compress, true))
                            {
                                Modded_Content(Compress);
                            }
                        }
                    }
                };
            }
        }
        /// <summary>
        /// Path String Checks to allow Certain Urls to Pass with Checks
        /// </summary>
        /// <remarks>Allow URL Only Requests with No Body Response</remarks>
        /// <param name="Context_Request"></param>
        /// <returns></returns>
        private static bool Content_Request_ByPass(string Context_Request)
        {
            if (!string.IsNullOrWhiteSpace(Context_Request))
            {
                return Context_Request.Contains("/nfsw/Engine.svc/User/SecureLoginPersona") || 
                    Context_Request.Contains("/nfsw/Engine.svc/User/SecureLogout") ||
                   Context_Request.Contains("/nfsw/Engine.svc/events/notifycoincollected") || 
                   Context_Request.Contains("/nfsw/Engine.svc/DriverPersona/UpdatePersonaPresence") ||
                   Context_Request.Contains("/nfsw/Engine.svc/matchmaking/joinqueueracenow") || 
                   Context_Request.Contains("/nfsw/Engine.svc/matchmaking/leavequeue") ||
                   Context_Request.Contains("/nfsw/Engine.svc/event/launched") || 
                   Context_Request.Contains("/nfsw/Engine.svc/powerups/activated") ||
                   Context_Request.Contains("/nfsw/Engine.svc/car/repair") ||
                   Context_Request.Contains("/nfsw/Engine.svc/matchmaking/leavelobby") ||
                   Context_Request.Contains("/nfsw/Engine.svc/Reporting/SendClientPingTime") ||
                   Context_Request.Contains("/nfsw/Engine.svc/matchmaking/declineinvite");
            }

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        private static bool ContentLengthIsTooSmall(NancyContext Context)
        {
            try
            {
                if (Context.Response.Headers == null)
                {
                    if (Launcher_Value.Launcher_Insider_Dev) { Log.Debug("Headers is Null for " + Context.Request.Path); }
                    return true;
                }
                else
                {
                    if (!Context.Response.Headers.TryGetValue("Content-Length", out string ContentLength))
                    {
                        using (MemoryStream mm = new MemoryStream())
                        {
                            Context.Response.Contents.Invoke(mm);
                            mm.Flush();
                            ContentLength = mm.Length.ToString();
                        }
                    }
                    if (Launcher_Value.Launcher_Insider_Dev) { Log.Debug($"GZip Content-Length of response is {ContentLength} for {Context.Request.Path}"); }

                    /* Wine Mono is Unable to Allow the Game to Continue compared to its Windows CounterPart OR
                     * Allow URL Only Requests with No Body Response */
                    if (Content_Request_ByPass(Context.Request.Path) || long.Parse(ContentLength) > 0 || Launcher_Value.System_Unix)
                    {
                        return false;
                    }
                    else
                    {
                        if (Launcher_Value.Launcher_Insider_Dev) { Log.Debug($"GZip Content-Length is too small for {Context.Request.Path}"); }
                        return true;
                    }
                }
            }
            catch (Exception Error)
            {
                Log_Detail.Full("ContentLengthIsTooSmall", Error);
                return true;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        private static bool ResponseIsCompressed(NancyContext Context)
        {
            bool Status = false;
            try
            {
                if (Context.Response.Headers.Keys != null)
                {
                    Status = Context.Response.Headers.Keys.Any(x => x.Contains("Content-Encoding"));
                }

                if (Launcher_Value.Launcher_Insider_Dev && !Status) { Log.Debug("Is Compressed? For " + Context.Request.Path + " " + Status); }
            }
            catch (Exception Error)
            {
                Log_Detail.Full("ResponseIsCompressed", Error);
            }

            return Status;
        }
        /// <summary>
        /// 
        /// </summary>
        private static IList<string> MimeTypes { get; set; } = new List<string>
        {
            "text/plain",
            "text/html",
            "text/xml",
            "text/css",
            "application/json",
            "application/x-javascript",
            "application/atom+xml",
            "application/xml;charset=UTF-8",
            "application/xml"
        };
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        private static bool ResponseIsCompatibleMimeType(NancyContext Context)
        {
            bool Status = false;
            try
            {
                if (Context.Response.ContentType != null)
                {
                    if (MimeTypes.Any(x => x == Context.Response.ContentType))
                    {
                        Status = true;
                    }
                    else if (MimeTypes.Any(x => Context.Response.ContentType.StartsWith($"{x};")))
                    {
                        Status = true;
                    }
                }

                if (Launcher_Value.Launcher_Insider_Dev && !Status) { Log.Debug("Content Type? For " + Context.Request.Path + " " + Status); }
            }
            catch (Exception Error)
            {
                Log_Detail.Full("ResponseIsCompatibleMimeType", Error);
            }

            return Status;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Context"></param>
        /// <returns></returns>
        private static bool RequestIsGzipCompatible(NancyContext Context)
        {
            bool Status = false;
            try
            {
                if (Context.Request.Headers.AcceptEncoding != null)
                {
                    if (Context.Request.Headers.AcceptEncoding.Any(x => x.Contains("gzip")))
                    {
                        Status = true;
                    }
                    else if (Context.Request.Headers.AcceptEncoding.Any(x => x.Contains("deflate")))
                    {
                        Status = true;
                    }
                }

                if (Launcher_Value.Launcher_Insider_Dev && !Status) { Log.Debug("Gzip Compatible? For " + Context.Request.Path + " " + Status); }
            }
            catch (Exception Error)
            {
                Log_Detail.Full("RequestIsGzipCompatible", Error);
            }

            return Status;
        }
    }
}
