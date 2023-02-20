using SBRW.Launcher.Core.Extension.Logging_;
using SBRW.Launcher.Core.Proxy.Singleton_Instance;
using SBRW.Nancy.Hosting.Self;
using System;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    /// <summary>
    /// Proxy Class Built With Nancy
    /// </summary>
    /// <remarks><i>SBRW Requires <b>Nancy Self-Hosted</b> Library</i></remarks>
    public class Proxy_Server : Singleton<Proxy_Server>
    {
        /// <summary>
        /// Proxy Service
        /// </summary>
        internal static NancyHost Host_Service { get; set; }
        /// <summary>
        /// Starts the Proxy Server
        /// </summary>
        /// <param name="From">Where the function is Called</param>
        public void Start(string From)
        {
            try
            {
                if (Proxy_Settings.Running())
                {
                    Log.Warning("PROXY: Local Proxy Server already Running! (" + From + ")");
                }
                else
                {
                    HostConfiguration Configs = new HostConfiguration()
                    {
                        AllowChunkedEncoding = false,
                        RewriteLocalhost = Proxy_Settings.Rewrite_Localhost,
                        UrlReservations = new UrlReservations()
                        {
                            CreateAutomatically = true
                        }
                    };

                    Host_Service = new NancyHost(new Uri("http://" + Proxy_Settings.Domain + (Proxy_Settings.Portless ? "" : ":" + Proxy_Settings.Custom_Port())), new Nancy_Bootstrapper(), Configs);
                    Host_Service.Start();

                    Log.Info("PROXY: Local Proxy Server has Fully Initialized (" + From + ")");
                }
            }
            catch (AutomaticUrlReservationCreationFailureException Error)
            {
                Log_Detail.Full("PROXY [U.R.]", Error);
            }
            catch (Exception Error)
            {
                Log_Detail.Full("PROXY", Error);
            }
        }
        /// <summary>
        /// Starts the Proxy Server
        /// </summary>
        /// <remarks>Provides a Genric <i>From Location</i> Call in the Log</remarks>
        public void Start()
        {
            Start("Genric Start");
        }
        /// <summary>
        /// Stops the Proxy Server
        /// </summary>
        /// <param name="From">Where the function is Called</param>
        public void Stop(string From)
        {
            if (Proxy_Settings.Running())
            {
                Log.Info("PROXY: Local Proxy Server has Shutdown (" + From + ")");
                Host_Service.Stop();
                Host_Service.Dispose();
                Host_Service = null;
            }
            else
            {
                Log.Warning("PROXY: Local Proxy Server is already Shutdown (" + From + ")");
            }
        }
        /// <summary>
        /// Stops the Proxy Server
        /// </summary>
        /// <remarks>Provides a Genric <i>From Location</i> Call in the Log</remarks>
        public void Stop()
        {
            Stop("Genric Stop");
        }
    }
}
