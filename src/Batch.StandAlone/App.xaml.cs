﻿//*********************************************************************
//CAD+ Toolset
//Copyright(C) 2020 Xarial Pty Limited
//Product URL: https://cadplus.xarial.com
//License: https://cadplus.xarial.com/license/
//*********************************************************************

using Autofac;
using Autofac.Core;
using CommandLine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using Xarial.CadPlus.Batch.Base.Models;
using Xarial.CadPlus.Common;
using Xarial.CadPlus.Common.Services;
using Xarial.CadPlus.Plus;
using Xarial.CadPlus.Plus.Applications;
using Xarial.CadPlus.Plus.Services;
using Xarial.CadPlus.XBatch.Base;
using Xarial.CadPlus.XBatch.Base.Core;
using Xarial.CadPlus.XBatch.Base.Models;
using Xarial.CadPlus.XBatch.Base.Services;
using Xarial.CadPlus.XBatch.Base.ViewModels;
using Xarial.XCad.Base;

namespace Xarial.CadPlus.XBatch.Base
{
    public class BatchApplication : IBatchApplication
    {
        public Guid Id => Guid.Parse(ApplicationIds.BatchStandAlone);

        public IApplicationProvider[] ApplicationProviders => m_ApplicationProviders.ToArray();

        private readonly List<IApplicationProvider> m_ApplicationProviders;

        internal BatchApplication() 
        {
            m_ApplicationProviders = new List<IApplicationProvider>();
        }

        public void RegisterApplicationProvider(IApplicationProvider provider)
        {
            m_ApplicationProviders.Add(provider);
        }
    }

    public class XBatchApp : MixedApplication<IArguments>
    {
        private const int MAX_RETRIES = 2;

        private FileOptions m_StartupOptions;

        private readonly BatchApplication m_BatchApp;

        public XBatchApp() : this(new BatchApplication())
        {
        }

        public XBatchApp(BatchApplication batchApp) : base(batchApp)
        {
            m_BatchApp = batchApp;
        }

        protected override void OnAppStart()
        {
            this.StartupUri = new Uri("/batchplus;component/MainWindow.xaml", UriKind.Relative);
        }

        protected override void OnWindowStarted()
        {
            if (m_StartupOptions != null)
            {
                var vm = (BatchManagerVM)this.MainWindow.DataContext;

                if (!string.IsNullOrEmpty(m_StartupOptions.FilePath))
                {
                    vm.OpenDocument(m_StartupOptions.FilePath);
                }

                if (m_StartupOptions.CreateNew)
                {
                    vm.NewDocument();
                }
            }
        }

        protected override Task RunConsole(IArguments args)
        {
            return RunConsoleBatch(args);
        }

        protected override void OnConfigureServices(ContainerBuilder builder)
        {
            builder.RegisterType<RecentFilesManager>()
                .As<IRecentFilesManager>();

            builder.RegisterType<AppLogger>().As<IXLogger>();
            builder.RegisterType<BatchRunner>();
            builder.RegisterType<BatchRunnerModel>().As<IBatchRunnerModel>();
            builder.RegisterType<BatchRunJobExecutor>().As<IBatchRunJobExecutor>();
            builder.RegisterType<BatchManagerVM>();
            builder.RegisterType<PollyResilientWorker<BatchJobContext>>()
                .As<IResilientWorker<BatchJobContext>>()
                .WithParameter(new TypedParameter(typeof(int), MAX_RETRIES));
            builder.RegisterType<PopupKiller>().As<IPopupKiller>();
            builder.RegisterType<BatchDocumentVM>();

            builder.RegisterInstance(m_BatchApp)
                .AsSelf()
                .As<IBatchApplication>();

            builder.RegisterAdapter<IBatchApplication, IApplicationProvider[]>(x => x.ApplicationProviders);
            
            builder.RegisterType<JobManager>().As<IJobManager>()
                .SingleInstance()
                .OnActivating(x =>
                {
                    try
                    {
                        x.Instance.Init();
                    }
                    catch (Exception ex)
                    {
                        var logger = x.Context.Resolve<IXLogger>();
                        logger.Log(ex);
                    }
                });
        }

        private async Task RunConsoleBatch(IArguments args)
        {
            using (var batchRunner = m_Container.Resolve<BatchRunner>(
                new TypedParameter[]
                {
                    new TypedParameter(typeof(TextWriter), Console.Out),
                    new TypedParameter(typeof(IProgressHandler), new ConsoleProgressWriter())
                }))
            {
                await batchRunner.BatchRun((BatchJob)args).ConfigureAwait(false);
            }
        }

        protected override void TryExtractCliArguments(Parser parser, string[] input,
            out IArguments args, out bool hasArguments, out bool hasError)
        {
            args = default;
            hasError = false;
            hasArguments = false;

            if (input.Any())
            {
                IArguments argsLocal = default;
                bool hasErrorLocal = false;
                bool hasArgumentsLocal = false;

                parser.ParseArguments<FileOptions, RunOptions, JobOptions>(input)
                    .WithParsed<RunOptions>(a => { argsLocal = a; hasArgumentsLocal = true; })
                    .WithParsed<JobOptions>(a => { argsLocal = a; hasArgumentsLocal = true; })
                    .WithParsed<FileOptions>(a => m_StartupOptions = a)
                    .WithNotParsed(err => { hasErrorLocal = true; hasArgumentsLocal = true; });

                args = argsLocal;
                hasError = hasErrorLocal;
                hasArguments = hasArgumentsLocal;
            }
        }
    }
}