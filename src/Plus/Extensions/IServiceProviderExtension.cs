﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xarial.CadPlus.Plus.Extensions
{
    public static class IServiceProviderExtension
    {
        public static TSvc GetService<TSvc>(this IServiceProvider svcProv)
            => (TSvc)svcProv.GetService(typeof(TSvc));
    }
}
