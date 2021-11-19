using Flurl;
using Flurl.Http;
using Flurl.Http.Content;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Extensions;
using Nancy.Responses;
using SBRW.Launcher.Core.Classes.Cache;
using SBRW.Launcher.Core.Classes.Extension.Logging_;
using SBRW.Launcher.Core.Classes.Required.Anti_Cheat;
using SBRW.Launcher.Core.Discord.Discord_.RPC_;
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
    internal class Proxy_Request : IApplicationStartup
    {
        private readonly UTF8Encoding UTF8 = new UTF8Encoding(false);

        public void Initialize(IPipelines Data_Pipelines)
        {
            Data_Pipelines.BeforeRequest += ProxyRequest;
            Data_Pipelines.OnError += OnError;
        }

        private TextResponse OnError(NancyContext context, Exception Error)
        {
            Log.Error("PROXY HANDLER: " + context.Request.Path);
            Log_Detail.OpenLog("PROXY HANDLER", null, Error, null, true);

            Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "LAUNCHER", CommunicationLogEntryType.Error,
                new CommunicationLogLauncherError(Error.Message, context.Request.Path, context.Request.Method));

            context.Request.Dispose();

            return new TextResponse(HttpStatusCode.BadRequest, Error.Message);
        }

        private async Task<Response> ProxyRequest(NancyContext Local_Context, CancellationToken cancellationToken)
        {
            string path = Local_Context.Request.Path;
            string method = Local_Context.Request.Method.ToUpperInvariant();

            if (!path.StartsWith("/nfsw/Engine.svc"))
            {
                Log.Error("PROXY HANDLER: Invalid Request: " + path);
                return "Soapbox Race World: Launcher Proxy Core Version " + AssemblyName.GetAssemblyName("SBRW.Launcher.Core.Proxy.dll").Version.ToString()??string.Empty;
            }
            else
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
                        (header.Key, (header.Key == "Host") ? resolvedUrl.ToUri().Host : ((header.Value != null) ? header.Value.First() : string.Empty));
                }

                string requestBody = (method != "GET") ? Local_Context.Request.Body.AsString(UTF8) : string.Empty;

                Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "SERVER", CommunicationLogEntryType.Request,
                    new CommunicationLogRequest(requestBody, resolvedUrl.ToString(), method));

                IFlurlResponse responseMessage;

                if (path == "/event/arbitration" && !string.IsNullOrWhiteSpace(requestBody))
                {
                    requestBody = requestBody.Replace("</TopSpeed>", "</TopSpeed><Konami>" + AC_Core.Status_Convert() + "</Konami>");
                    foreach (var header in Local_Context.Request.Headers)
                    {
                        if (header.Key.ToLowerInvariant() == "content-length")
                        {
                            int KonamiCode = Convert.ToInt32(header.Value.First()) +
                                ("<Konami>" + AC_Core.Status_Convert() + "</Konami>").Length;
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

                string responseBody = await responseMessage.GetStringAsync();

                int statusCode = responseMessage.StatusCode;

                Presence_Game.HandleGameState(path, responseBody, Local_Context.Request.Query);

                TextResponse Response = new TextResponse(responseBody,
                    responseMessage.ResponseMessage.Content.Headers.ContentType?.MediaType ?? "application/xml;charset=UTF-8")
                {
                    StatusCode = (HttpStatusCode)statusCode
                };

                Communication_Nancy.RecordEntry(Launcher_Value.Game_Server_Name, "SERVER", CommunicationLogEntryType.Response,
                    new CommunicationLogResponse(responseBody, resolvedUrl.ToString(), method));

                return Response;
            }
        }
    }
}
