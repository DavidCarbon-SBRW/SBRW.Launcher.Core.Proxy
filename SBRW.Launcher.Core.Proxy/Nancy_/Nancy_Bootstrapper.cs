using SBRW.Nancy;
using SBRW.Nancy.Configuration;
using System;
using System.Collections.Generic;

namespace SBRW.Launcher.Core.Proxy.Nancy_
{
    internal class Nancy_Bootstrapper : DefaultNancyBootstrapper
    {
        protected override IEnumerable<Type> ViewEngines
        {
            get { return new List<Type>(); }
        }

        public override void Configure(INancyEnvironment environment)
        {
            base.Configure(environment);
            environment.Tracing(true, true);
        }
    }
}
