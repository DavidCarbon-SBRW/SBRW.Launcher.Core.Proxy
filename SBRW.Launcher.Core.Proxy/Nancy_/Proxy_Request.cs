﻿using Flurl;
using Flurl.Http;
using Flurl.Http.Content;
using SBRW.Nancy;
using SBRW.Nancy.Bootstrapper;
using SBRW.Nancy.Extensions;
using SBRW.Nancy.Responses;
using SBRW.Launcher.Core.Cache;
using SBRW.Launcher.Core.Extension.Logging_;
using SBRW.Launcher.Core.Required.Anti_Cheat;
using SBRW.Launcher.Core.Discord.RPC_;
using SBRW.Launcher.Core.Proxy.Log_;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UrlFlurl = Flurl.Url;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    /// <summary>
    /// 
    /// </summary>
    public class Proxy_Request : IApplicationStartup
    {
        private UTF8Encoding UTF8
        {
            get
            {
                return new UTF8Encoding(false);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="Data_Pipelines"></param>
        public void Initialize(IPipelines Data_Pipelines)
        {
            Data_Pipelines.BeforeRequest += ProxyRequest;
            Data_Pipelines.OnError += OnError;
        }

        private TextResponse OnError(NancyContext context, Exception Error)
        {
            Log.Error("PROXY HANDLER: " + context.Request.Path);
            Log_Detail.Full("PROXY HANDLER", Error);

            if (Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.Errors) || Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.All))
            {
                Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "LAUNCHER", CommunicationLogEntryType.Error,
                new CommunicationLogLauncherError(Error.Message, context.Request.Path, context.Request.Method));
            }

            context.Request.Dispose();

            return new TextResponse(!Proxy_Settings.Ignore_Errors ? HttpStatusCode.BadRequest : HttpStatusCode.OK, Error.Message);
        }

        private async Task<Response> ProxyRequest(NancyContext Local_Context, CancellationToken cancellationToken)
        {
            string path = Local_Context.Request.Path;
            string method = Local_Context.Request.Method.ToUpperInvariant();

            if (!path.StartsWith("/nfsw/Engine.svc"))
            {
                Log.Error("PROXY HANDLER: Invalid Request: " + path);
                return "Soapbox Race World: Launcher Proxy Core Version " + 
                    AssemblyName.GetAssemblyName("SBRW.Launcher.Core.Proxy.dll").Version.ToString()??string.Empty;
            }
            else
            {
                string responseBody = string.Empty;

                try
                {
                    path = path.Substring("/nfsw/Engine.svc".Length);

                    UrlFlurl resolvedUrl = new UrlFlurl(Launcher_Value.Game_Server_IP).AppendPathSegment(path, false);

                    foreach (var queryParamName in Local_Context.Request.Query)
                    {
                        resolvedUrl = resolvedUrl.SetQueryParam(queryParamName, Local_Context.Request.Query[queryParamName],
                            NullValueHandling.Ignore);
                    }

                    IFlurlRequest request = resolvedUrl.AllowAnyHttpStatus();

                    foreach (var header in Local_Context.Request.Headers)
                    {
                        /* Don't send Content-Length for GET requests - HeyItsLeo */
                        if (method == "GET" && header.Key.ToLowerInvariant() == "content-length")
                        {
                            continue;
                        }

                        request = request.WithHeader
                            (header.Key, (header.Key == "Host") ? resolvedUrl.ToUri().Host : 
                            ((header.Value != null) ? header.Value.First() : string.Empty));
                    }

                    string requestBody = (method != "GET") ? Local_Context.Request.Body.AsString(UTF8) : string.Empty;

                    if (Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.Requests) || Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.All))
                    {
                        Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "SERVER", CommunicationLogEntryType.Request,
                        new CommunicationLogRequest(requestBody, resolvedUrl.ToString(), method));
                    }

                    IFlurlResponse responseMessage;

                    if (path == "/event/arbitration" && !string.IsNullOrWhiteSpace(requestBody))
                    {
                        requestBody = requestBody.Replace("</TopSpeed>", 
                            "</TopSpeed><Konami>" + AC_Core.Status_Convert() + "</Konami><DiscordUID>" + 
                            Launcher_Value.Launcher_Discord_UserID + "</DiscordUID>");
                        foreach (var header in Local_Context.Request.Headers)
                        {
                            if (header.Key.ToLowerInvariant() == "content-length")
                            {
                                int KonamiCode = Convert.ToInt32(header.Value.First()) +
                                    ("<Konami>" + AC_Core.Status_Convert() + "</Konami><DiscordUID>" +
                            Launcher_Value.Launcher_Discord_UserID + "</DiscordUID>").Length;
                                request = request.WithHeader(header.Key, KonamiCode);
                            }
                        }
                    }

                    switch (method)
                    {
                        case "GET":
                            responseMessage = await request.GetAsync(cancellationToken);
                            break;
                        case "POST":
                            responseMessage = await request.PostAsync(new CapturedStringContent(requestBody),
                                cancellationToken);
                            break;
                        case "PUT":
                            responseMessage = await request.PutAsync(new CapturedStringContent(requestBody),
                                cancellationToken);
                            break;
                        case "DELETE":
                            responseMessage = await request.DeleteAsync(cancellationToken);
                            break;
                        default:
                            Log.Error("PROXY HANDLER: Cannot handle Request Method " + method);
                            responseMessage = null;
                            break;
                    }

                    responseBody = await responseMessage.GetStringAsync();

                    int statusCode = responseMessage.StatusCode;

                    TextResponse Response = new TextResponse(responseBody,
                        responseMessage.ResponseMessage.Content.Headers.ContentType?.MediaType ?? "application/xml;charset=UTF-8")
                    {
                        StatusCode = (HttpStatusCode)statusCode
                    };

                    if (Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.Responses) || Proxy_Settings.Log_Mode.Equals(CommunicationLogRecord.All))
                    {
                        Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "SERVER", CommunicationLogEntryType.Response,
                        new CommunicationLogResponse(responseBody, resolvedUrl.ToString(), method));
                    }

                    return Response;
                }
                finally
                {
                    Presence_Game.State_Async(path, responseBody, Local_Context.Request.Query);
                }
            }
        }
    }
}
