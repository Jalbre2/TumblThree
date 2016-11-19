﻿using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Waf.Applications;
using System.Windows.Threading;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Domain;
using TumblThree.Domain.Queue;

namespace TumblThree.Applications.Controllers
{
    [Export(typeof(IModuleController)), Export]
    internal class ModuleController : IModuleController
    {
        private const string appSettingsFileName = "Settings.xml";
        private const string queueSettingsFileName = "Queuelist.xml";
        private const string managerSettingsFileName = "Manager.xml";

        private readonly Lazy<ShellService> shellService;
        private readonly IEnvironmentService environmentService;
        private readonly ISettingsProvider settingsProvider;
        private readonly Lazy<ManagerController> managerController;
        private readonly Lazy<QueueController> queueController;
        private readonly Lazy<DetailsController> detailsController;
        private readonly Lazy<ShellViewModel> shellViewModel;
        private readonly QueueManager queueManager;
        private AppSettings appSettings;
        private QueueSettings queueSettings;
        private ManagerSettings managerSettings;


        [ImportingConstructor]
        public ModuleController(Lazy<ShellService> shellService, IEnvironmentService environmentService, ISettingsProvider settingsProvider, Lazy<ManagerController> managerController,
            Lazy<QueueController> queueController, Lazy<DetailsController> detailsController, Lazy<ShellViewModel> shellViewModel)
        {
            this.shellService = shellService;
            this.environmentService = environmentService;
            this.settingsProvider = settingsProvider;
            this.detailsController = detailsController;
            this.managerController = managerController;
            this.queueController = queueController;
            this.shellViewModel = shellViewModel;
            this.queueManager = new QueueManager();
        }


        private ShellService ShellService { get { return shellService.Value; } }

        private ManagerController ManagerController { get { return managerController.Value; } }

        private QueueController QueueController { get { return queueController.Value; } }

        private DetailsController DetailsController { get { return detailsController.Value; } }

        private ShellViewModel ShellViewModel { get { return shellViewModel.Value; } }


        public void Initialize()
        {
            appSettings = LoadSettings<AppSettings>(appSettingsFileName);
            queueSettings = LoadSettings<QueueSettings>(queueSettingsFileName);
            managerSettings = LoadSettings<ManagerSettings>(managerSettingsFileName);

            ShellService.Settings = appSettings;
            ShellService.ShowErrorAction = ShellViewModel.ShowError;
            ShellService.ShowDetailsViewAction = ShowDetailsView;
            ShellService.ShowQueueViewAction = ShowQueueView;
            ShellService.InitializeOAuthManager();

            ManagerController.QueueManager = queueManager;
            ManagerController.ManagerSettings = managerSettings;
            ManagerController.Initialize();
            QueueController.QueueSettings = queueSettings;
            QueueController.QueueManager = queueManager;
            QueueController.Initialize();
            DetailsController.QueueManager = queueManager;
            DetailsController.Initialize();

            ManagerController.BlogManagerFinishedLoading += OnBlogManagerFinishedLoading;
        }

        public async void Run()
        {
            ShellViewModel.IsQueueViewVisible = true;
            ShellViewModel.Show();

            // Let the UI to initialize first before loading the queuelist.
            await Dispatcher.CurrentDispatcher.InvokeAsync(QueueController.Run, DispatcherPriority.ApplicationIdle);
        }

        private void OnBlogManagerFinishedLoading(object sender, EventArgs e)
        {
            QueueController.LoadQueue();
        }

        public void Shutdown()
        {
            DetailsController.Shutdown();
            QueueController.Shutdown();
            ManagerController.Shutdown();

            SaveSettings(appSettingsFileName, appSettings);
            SaveSettings(queueSettingsFileName, queueSettings);
            SaveSettings(managerSettingsFileName, managerSettings);
        }

        private T LoadSettings<T>(string fileName) where T : class, new()
        {
            try
            {
                return settingsProvider.LoadSettings<T>(Path.Combine(environmentService.AppSettingsPath, fileName));
            }
            catch (Exception ex)
            {
                Logger.Error("Could not read the settings file: {0}", ex);
                return new T();
            }
        }

        private void SaveSettings(string fileName, object settings)
        {
            try
            {
                settingsProvider.SaveSettings(Path.Combine(environmentService.AppSettingsPath, fileName), settings);
            }
            catch (Exception ex)
            {
                Logger.Error("Could not save the settings file: {0}", ex);
            }
        }

        private void ShowDetailsView()
        {
            ShellViewModel.IsDetailsViewVisible = true;
        }

        private void ShowQueueView()
        {
            ShellViewModel.IsQueueViewVisible = true;
        }
    }
}
