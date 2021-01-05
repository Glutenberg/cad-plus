﻿//*********************************************************************
//CAD+ Toolset
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://cadplus.xarial.com
//License: https://cadplus.xarial.com/license/
//*********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xarial.CadPlus.Plus
{
    public interface IContainerBuilder
    {
        IServiceProvider Build();
        
        void Register<TImplementer, TService>()
            where TImplementer : TService;

        void RegisterInstance<TInstance, TService>(TInstance inst)
            where TInstance : class
            where TService : TInstance;
    }
}
