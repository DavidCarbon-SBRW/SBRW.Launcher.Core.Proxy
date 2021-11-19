using Nancy.Hosting.Self;
using SBRW.Launcher.Core.Classes.Cache;
using SBRW.Launcher.Core.Classes.Extension.Logging_;
using SBRW.Launcher.Core.Proxy.Singleton_Instance;
using System;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    /// <summary>
    /// Proxy Class Built With Nancy
    /// </summary>
    /// <remarks><i>Requires <b>Nancy Self-Hosted</b> Library</i></remarks>
    public class Proxy_Server : Singleton<Proxy_Server>
    {
        internal static NancyHost Host { get; set; }
        /// <summary>
        /// Boolean Value on Launcher Proxy if its Running
        /// </summary>
        /// <returns>True or False</returns>
        public static bool Running() => Launcher_Value.Launcher_Proxy = Host != null;
        /// <summary>
        /// Starts the Proxy Server
        /// </summary>
        /// <param name="From">Where the function is Called</param>
        public void Start(string From)
        {
            try
            {
                if (Running())
                {
                    Log.Warning("PROXY: Local Proxy Server already Running! (" + From + ")");
                }
                else
                {
                    Log.Info("PROXY: Local Proxy Server has Fully Initialized (" + From + ")");

                    HostConfiguration Configs = new HostConfiguration()
                    {
                        AllowChunkedEncoding = false,
                        RewriteLocalhost = false,
                        UrlReservations = new UrlReservations()
                        {
                            CreateAutomatically = true
                        }
                    };

                    Host = new NancyHost(new Uri("http://127.0.0.1:" + Launcher_Value.Launcher_Proxy_Port), new Nancy_Bootstrapper(), Configs);
                    Host.Start();
                }
            }
            catch (AutomaticUrlReservationCreationFailureException Error)
            {
                Log_Detail.OpenLog("PROXY [U.R.]", null, Error, null, true);
            }
            catch (Exception Error)
            {
                Log_Detail.OpenLog("PROXY", null, Error, null, true);
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
            if (Running())
            {
                Log.Info("PROXY: Local Proxy Server has Shutdown (" + From + ")");
                Host.Stop();
                Host.Dispose();
                Host = null;
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
