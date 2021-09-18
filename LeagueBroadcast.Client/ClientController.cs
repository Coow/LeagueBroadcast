using Client.MVVM.View;
using Client.MVVM.View.Startup;
using Client.MVVM.ViewModel;
using Common;
using Common.Config;
using Common.Config.Files;
using Common.Exceptions;
using Server;
using Update;
using Utils;
using Utils.Log;

using System;
using System.Windows;
using Trinket;

namespace Client
{
    public class ClientController
    {
        public static event EventHandler? PreInitComplete, InitComplete, LateInitComplete;
        public static StartupViewModel? StartupCtx { get; private set; }
        public static MainViewModel? MainCtx { get; private set; }

        public static MainWindow? MainWin { get; private set; }
        public static StartupWindow? StartupWin { get; private set; }


        private static DateTime lastInitStepStartTime;
        private static AppConfig? appConfig;
        private static ClientConfig? clientConfig;

        static ClientController()
        {
            AppStatus.LoadStatusUpdate += StatusUpdate;
            AppStatus.LoadProgressUpdate += ProgressUpdate;
        }

        public static async void PreInit()
        {
            lastInitStepStartTime = DateTime.Now;

            //Init logging
            _ = Logger.RegisterLogger<InstanceFileLogger>();

            //Create Startup window
            StartupWin = new StartupWindow();
            StartupWin.Show();
            StartupCtx = (StartupViewModel)StartupWin.DataContext;
            $"Starting LeagueBroadcast Client v{StringVersion.CallingAppVersion}".Info();

            //Load app and client configs before anything else since this determines how to proceed with loading
            if (!(await ConfigController.RegisterConfigAsync<AppConfig>() && await ConfigController.RegisterConfigAsync<ClientConfig>()))
            {
                //Something went very wrong...
                throw new InvalidConfigException("Could not load main config file. App cannot run");
            }
            appConfig = ConfigController.Get<AppConfig>();
            clientConfig = ConfigController.Get<ClientConfig>();

            //Check for updates
            if (await UpdateController.CheckForUpdate("LeagueBroadcast", clientConfig.LastSkippedVersion))
            {
                //No point in continuing startup if we are going to restart in a moment anyway
                UpdateController.UpdateDownloaded += (s, e) =>
                {
                    UpdateController.RestartApplication();
                };
                return;
            }


            //Start server on local or connect to remote?

            if (!appConfig.SaveRunServerOption)
            {
                //TODO
            }
            if (appConfig.RunServer)
            {
                $"Local Server enabled. Starting Server".Info();
                ServerController.Startup();
                ServerController.InitComplete += (s, e) => Init();
                ServerController.LateInitComplete += (s, e) => LateInit();
            }
            else
            {
                PreInitComplete += (s, e) => Init();
                InitComplete += (s, e) => LateInit();
            }
            PreInitComplete?.Invoke(null, EventArgs.Empty);
        }

        public static void Init()
        {
            LiveEventDataProvider.Instance.OnConnect += (s, e) => "LiveEvent Connected".Info();
            LiveEventDataProvider.Instance.OnLiveEvent += (s, e) => { e.ToString().Info(); };
            LiveEventDataProvider.Instance.OnConnectionError += (s, e)  => { e.ToString().Info(); };

            InitComplete?.Invoke(null, EventArgs.Empty);
        }

        public static void LateInit()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                //Create Main window
                MainWin = new MainWindow();
                MainWin.Show();
                MainCtx = (MainViewModel)MainWin.DataContext;
                StartupWin?.Close();
            });
            LateInitComplete?.Invoke(null, EventArgs.Empty);
        }
        private static void StatusUpdate(object? sender, string status)
        {
            if (StartupCtx is not null)
                StartupCtx.StatusMessage = status;
            if (MainCtx is not null)
                MainCtx.StatusMessage = status;
        }

        private static void ProgressUpdate(object? sender, LoadProgressUpdateEventArgs e)
        {
            StartupCtx?.UpdateLoadProgress(e.Status, e.Progress);
        }
    }
}
