using SBRW.Nancy;
using SBRW.Nancy.Configuration;
using System;
using System.Collections.Generic;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    /// <summary>
    /// 
    /// </summary>
    public class Nancy_Bootstrapper : DefaultNancyBootstrapper
    {
        /// <summary>
        /// 
        /// </summary>
        protected override IEnumerable<Type> ViewEngines
        {
            get { return new List<Type>(); }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="environment"></param>
        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);
            environment.Tracing(true, true);
        }
    }
}
