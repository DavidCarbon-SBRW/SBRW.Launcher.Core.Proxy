using SBRW.Launcher.Core.Extension.Logging_;
using System;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    /// <summary>
    /// Central Proxy Settings
    /// </summary>
    public class Proxy_Settings
    {
        /// <summary>
        /// Informs the Proxy to Continue with Error
        /// </summary>
        /// <remarks>Sets the <see cref="SBRW.Nancy.HttpStatusCode">Status Code</see> to OK (200)</remarks>
        public static bool Ignore_Errors { get; set; }
        /// <summary>
        /// Boolean Value on Launcher Proxy if its Running
        /// </summary>
        /// <remarks><i>Requires <b>SBRW Nancy Self-Hosted</b> Library</i></remarks>
        /// <returns>True or False</returns>
        public static bool Running() => Proxy_Server.Host_Service != null;
        /// <summary>
        /// Cached Custom Port
        /// </summary>
        public static int Port { get; set; } = 2017;
        /// <summary>
        /// Set Proxy Port Number
        /// </summary>
        /// <param name="Custom_Port">Set a Custom Proxy Port</param>
        /// <returns>Return Set Proxy Port</returns>
        public static int Custom_Port(string Custom_Port = null)
        {
            bool UsingCustomProxyPort = false;

            if (!string.IsNullOrWhiteSpace(Custom_Port))
            {
                bool isNumeric = int.TryParse(Custom_Port, out int Converted_Port);

                if (isNumeric)
                {
                    if (Port > 0)
                    {
                        Port = Converted_Port;
                        UsingCustomProxyPort = true;
                        Log.Info("Proxy Settings:".ToUpper() + " Custom Proxy Port -> " + Port);
                    }
                }
            }

            if (!UsingCustomProxyPort)
            {
                bool isNumeric = int.TryParse(DateTime.Now.Year.ToString(), out int Converted_Port);

                if (isNumeric)
                {
                    Port = new Random().Next(2017, Converted_Port);
                }
                else
                {
                    Port = new Random().Next(2017, 2022);
                }

                Log.Info("Proxy Settings:".ToUpper() + " Random Generated Default Port -> " + Port);
            }

            return Port;
        }
    }
}
