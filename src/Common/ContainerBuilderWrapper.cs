﻿using Autofac;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xarial.CadPlus.Plus;

namespace Xarial.CadPlus.Common
{
    public class ContainerBuilderWrapper : IContainerBuilder
    {
        public ContainerBuilder Builder { get; }

        public ContainerBuilderWrapper(ContainerBuilder builder) 
        {
            Builder = builder;
        }

        public void Register<TImplementer, TService>() where TImplementer : TService
            => Builder.RegisterType<TImplementer>().As<TService>();
    }
}
