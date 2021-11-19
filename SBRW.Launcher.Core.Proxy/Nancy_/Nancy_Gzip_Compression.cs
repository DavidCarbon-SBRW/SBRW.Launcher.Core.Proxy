using Nancy;
using Nancy.Bootstrapper;
using Nancy.Responses;
using SBRW.Launcher.Core.Classes.Cache;
using SBRW.Launcher.Core.Classes.Extension.Logging_;
using SBRW.Launcher.Core.Classes.Required.Anti_Cheat;
using SBRW.Launcher.Core.Proxy.Log_;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    internal class Nancy_Gzip_Compression : IApplicationStartup
    {
        public void Initialize(IPipelines Data_Pipelines)
        {
            Data_Pipelines.AfterRequest += CheckForCompression;
            Data_Pipelines.OnError += OnError;
        }

        private TextResponse OnError(NancyContext context, Exception Error)
        {
            Log.Error("PROXY HANDLER: " + context.Request.Path);
            Log_Detail.OpenLog("PROXY HANDLER", null, Error, null, true);

            Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "PROXY", CommunicationLogEntryType.Error,
                new CommunicationLogLauncherError(Error.Message, context.Request.Path, context.Request.Method));

            context.Request.Dispose();

            return new TextResponse(HttpStatusCode.BadRequest, Error.Message);
        }

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

            Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "LAUNCHER", CommunicationLogEntryType.Rejected,
            new CommunicationLogLauncherError(ErrorReason, Context.Request.Path, Context.Request.Method));
        }

        private static void CheckForCompression(NancyContext Context)
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
            else if (!Launcher_Value.Game_In_Event && (Session_Timer.Remaining <= 0))
            {
                try
                {
                    Launcher_Value.Game_In_Event_Bug = true;
                    if (Launcher_Value.Game_Process != null)
                    {
                        if (!Launcher_Value.Game_Process.CloseMainWindow())
                        {
                            Launcher_Value.Game_Process.Kill();
                        }
                    }
                    else
                    {
                        Process[] allOfThem = Process.GetProcessesByName("nfsw");

                        if (allOfThem != null && allOfThem.Any())
                        {
                            foreach (var oneProcess in allOfThem)
                            {
                                Process.GetProcessById(oneProcess.Id).Kill();
                            }
                        }
                    }
                }
                catch { }
            }
            else
            {
                CompressResponse(Context);
            }
        }

        private static void CompressResponse(NancyContext Context)
        {
            bool Deflate = Context.Request.Headers.AcceptEncoding.Any(x => x.Contains("deflate"));

            Context.Response.Headers["Content-Encoding"] = Deflate ? "deflate" : "gzip";
            Context.Response.Headers["Connection"] = "close";

            var FinalResponse = Context.Response.Contents;

            Context.Response.Contents = responseStream =>
            {
                if (Deflate)
                {
                    using (DeflateStream Compressed = new DeflateStream(responseStream, CompressionLevel.Optimal, true))
                    {
                        FinalResponse(Compressed);
                    }
                }
                else
                {
                    using (GZipStream Compress = new GZipStream(responseStream, CompressionMode.Compress, true))
                    {
                        FinalResponse(Compress);
                    }
                }
            };

            using (MemoryStream mm = new MemoryStream())
            {
                Context.Response.Contents.Invoke(mm);
                mm.Flush();

                Context.Response.Headers["Content-Length"] = mm.Length.ToString();
            }

            /* Different Solutions With Different OoM Errors */
            /* https://gist.github.com/DavidCarbon/e0b37e7bc58b5e1a46f6dfedc87c966d */
        }

        private static bool ContentLengthIsTooSmall(NancyContext Context)
        {
            try
            {
                if (Context.Response.Headers == null)
                {
                    //if (EnableInsiderDeveloper.Allowed()) { Log.Debug("Headers is Null for " + Context.Request.Path); }
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
                    //if (EnableInsiderDeveloper.Allowed()) { Log.Debug($"GZip Content-Length of response is {ContentLength} for {Context.Request.Path}"); }

                    /* Wine Mono is Unable to Allow the Game to Continue compared to its Windows CounterPart */
                    if (long.Parse(ContentLength) > 0 || Launcher_Value.System_Unix)
                    {
                        return false;
                    }
                    else
                    {
                        //if (EnableInsiderDeveloper.Allowed()) { Log.Debug($"GZip Content-Length is too small for {Context.Request.Path}"); }
                        return true;
                    }
                }
            }
            catch (Exception Error)
            {
                Log_Detail.OpenLog("ContentLengthIsTooSmall", null, Error, null, true);
                return true;
            }
        }

        private static bool ResponseIsCompressed(NancyContext Context)
        {
            bool Status = false;
            try
            {
                if (Context.Response.Headers.Keys != null)
                {
                    Status = Context.Response.Headers.Keys.Any(x => x.Contains("Content-Encoding"));
                }

                //if (EnableInsiderDeveloper.Allowed() && !Status) { Log.Debug("Is Compressed? For " + Context.Request.Path + " " + Status); }
            }
            catch (Exception Error)
            {
                Log_Detail.OpenLog("ResponseIsCompressed", null, Error, null, true);
            }
            return Status;
        }

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

                //if (EnableInsiderDeveloper.Allowed() && !Status) { Log.Debug("Content Type? For " + Context.Request.Path + " " + Status); }
            }
            catch (Exception Error)
            {
                Log_Detail.OpenLog("ResponseIsCompatibleMimeType", null, Error, null, true);
            }
            return Status;
        }

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

                //if (EnableInsiderDeveloper.Allowed() && !Status) { Log.Debug("Gzip Compatible? For " + Context.Request.Path + " " + Status); }
            }
            catch (Exception Error)
            {
                Log_Detail.OpenLog("RequestIsGzipCompatible", null, Error, null, true);
            }
            return Status;
        }
    }
}
