﻿//*********************************************************************
//CAD+ Toolset
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://cadplus.xarial.com
//License: https://cadplus.xarial.com/license/
//*********************************************************************

using System;
using Xarial.XCad.Extensions;
using Xarial.CadPlus.Module.Init;
using Xarial.CadPlus.Plus;
using Xarial.XCad;
using Xarial.XCad.UI.Commands;
using Xarial.XCad.UI.Commands.Structures;
using System.Linq;
using System.Collections.Generic;
using Xarial.XCad.UI.PropertyPage;
using Xarial.XToolkit.Reflection;
using System.Reflection;
using System.ComponentModel.Composition;
using System.IO;
using System.ComponentModel.Composition.Primitives;
using System.ComponentModel.Composition.Hosting;
using Xarial.XToolkit.Wpf.Dialogs;
using Xarial.CadPlus.AddIn.Base.Properties;
using Xarial.XCad.Base;
using Xarial.CadPlus.Common.Services;
using Autofac;
using Xarial.CadPlus.Common;
using Autofac.Core.Registration;

namespace Xarial.CadPlus.AddIn.Base
{
    public delegate IXPropertyPage<TData> CreatePageDelegate<TData>();

    public class AddInHostApplication : BaseHostApplication, IHostExtensionApplication
    {
        [ImportMany]
        private IEnumerable<IExtensionModule> m_Modules;

        internal const int ROOT_GROUP_ID = 1000;

        public override IntPtr ParentWindow => Extension.Application.WindowHandle;

        public IXExtension Extension { get; }

        public override IEnumerable<IModule> Modules => m_Modules;

        public override event Action Connect;
        public override event Action Disconnect;
        public override event Action Initialized;
        public override event Action<IContainerBuilder> ConfigureServices;
        public override event Action Started;

        private CommandGroupSpec m_ParentGrpSpec;

        private int m_NextId;

        private readonly Dictionary<CommandSpec, Tuple<Delegate, Enum>> m_Handlers;
        
        public override IServiceProvider Services => m_SvcProvider;
        
        private ServiceProvider m_SvcProvider;

        private IPropertyPageCreator m_PageCreator;

        public AddInHostApplication(IXExtension ext) 
        {
            try
            {
                Extension = ext;
                m_NextId = ROOT_GROUP_ID + 1;

                m_Handlers = new Dictionary<CommandSpec, Tuple<Delegate, Enum>>();

                Extension.StartupCompleted += OnStartupCompleted;
                Extension.Connect += OnExtensionConnect;
                Extension.Disconnect += OnExtensionDisconnect;
                if (Extension is IXServiceConsumer)
                {
                    (Extension as IXServiceConsumer).ConfigureServices += OnConfigureExtensionServices;
                }

                var modulesDir = Path.Combine(Path.GetDirectoryName(this.GetType().Assembly.Location), "Modules");

                var catalog = CreateDirectoryCatalog(modulesDir, "*.Module.dll");

                var container = new CompositionContainer(catalog);
                container.SatisfyImportsOnce(this);

                if (m_Modules?.Any() == true)
                {
                    foreach (var module in m_Modules)
                    {
                        module.Init(this);
                    }
                }

                Initialized?.Invoke();
            }
            catch 
            {
                new GenericMessageService("CAD+").ShowError("Failed to init add-in");
                throw;
            }
        }

        private void OnExtensionDisconnect(IXExtension ext) => Dispose();

        private ComposablePartCatalog CreateDirectoryCatalog(string path, string searchPattern)
        {
            var catalog = new AggregateCatalog();

            catalog.Catalogs.Add(new DirectoryCatalog(path, searchPattern));

            foreach (var subDir in Directory.GetDirectories(path, "*.*", SearchOption.AllDirectories))
            {
                catalog.Catalogs.Add(new DirectoryCatalog(subDir, searchPattern));
            }

            return catalog;
        }

        private void OnStartupCompleted(IXExtension ext)
        {
            Started?.Invoke();
        }

        private void OnExtensionConnect(IXExtension ext) 
        {
            try
            {
                var cmdGrp = Extension.CommandManager.AddCommandGroup<CadPlusCommands_e>();
                cmdGrp.CommandClick += OnCommandClick;

                m_ParentGrpSpec = cmdGrp.Spec;

                Connect?.Invoke();
            }
            catch 
            {
                new GenericMessageService("CAD+").ShowError("Failed to connect add-in");
                throw;
            }
        }
        
        private void OnConfigureExtensionServices(IXServiceConsumer sender, IXServiceCollection svcColl)
        {
            var builder = new ContainerBuilder();

            builder.Populate(svcColl);

            ConfigureHostServices(builder);

            ConfigureServices?.Invoke(new ContainerBuilderWrapper(builder));

            m_SvcProvider = new ServiceProvider(builder.Build());

            svcColl.Populate(m_SvcProvider.Container);

            m_PageCreator = m_SvcProvider.Container.Resolve<IPropertyPageCreator>();
        }

        private void ConfigureHostServices(ContainerBuilder builder) 
        {   
            builder.RegisterInstance(Extension.Application);
            builder.RegisterType<AppLogger>().As<IXLogger>();
            builder.RegisterType<CadAppMessageService>()
                .As<IMessageService>();
        }
                
        public void RegisterCommands<TCmd>(CommandHandler<TCmd> handler)
            where TCmd : Enum
        {
            var spec = Extension.CommandManager.CreateSpecFromEnum<TCmd>(m_NextId++, m_ParentGrpSpec);
            spec.Parent = m_ParentGrpSpec;
            
            var grp = Extension.CommandManager.AddCommandGroup(spec);

            foreach (var cmd in grp.Spec.Commands) 
            {
                m_Handlers.Add(cmd, new Tuple<Delegate, Enum>(handler, (Enum)Enum.ToObject(typeof(TCmd), cmd.UserId)));
            }

            grp.CommandClick += OnCommandClick;
        }

        private void OnCommandClick(CommandSpec spec)
        {
            if (m_Handlers.TryGetValue(spec, out Tuple<Delegate, Enum> handler))
            {
                handler.Item1.DynamicInvoke(handler.Item2);
            }
            else 
            {
                System.Diagnostics.Debug.Assert(false, "Handler is not registered");
            }
        }

        public IXPropertyPage<TData> CreatePage<TData>()
            => m_PageCreator.CreatePage<TData>();

        private void OnCommandClick(CadPlusCommands_e spec)
        {
            switch (spec)
            {
                case CadPlusCommands_e.Help:
                    try
                    {
                        System.Diagnostics.Process.Start(Resources.HelpLink);
                    }
                    catch
                    {
                    }
                    break;

                case CadPlusCommands_e.About:
                    AboutDialog.Show(this.GetType().Assembly, Resources.logo,
                        Extension.Application.WindowHandle);
                    break;
            }
        }

        public override void Dispose()
        {
            Disconnect?.Invoke();

            if (m_Modules?.Any() == true)
            {
                foreach (var module in m_Modules)
                {
                    module.Dispose();
                }
            }
        }
    }
}
