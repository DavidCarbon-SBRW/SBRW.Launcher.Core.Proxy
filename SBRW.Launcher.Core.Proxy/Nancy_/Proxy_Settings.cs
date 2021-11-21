using SBRW.Launcher.Core.Classes.Extension.Logging_;
using System;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    /// <summary>
    /// Central Proxy Settings
    /// </summary>
    public class Proxy_Settings
    {
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
        public static int Custom_Port(string Custom_Port)
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
                bool isNumeric = int.TryParse(DateTime.Now.Year.ToString(), out int Port);

                if (isNumeric)
                {
                    Port = new Random().Next(2017, Port);
                }
                else
                {
                    Port = new Random().Next(2017, 2022);
                }

                Log.Info("Proxy Settings:".ToUpper() + " Random Generated Default Port -> " + Port);
            }

            return Port;
        }
        /// <summary>
        /// Set Proxy Port Number
        /// </summary>
        public static int Custom_Port()
        {
            return Custom_Port(null);
        }
    }
}
