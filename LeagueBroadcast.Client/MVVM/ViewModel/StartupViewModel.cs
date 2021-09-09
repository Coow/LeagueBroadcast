using Common;
using Server.Data;
using Update;
using Utils;
using System;
using System.Windows;
using static Common.LoadProgressUpdateEventArgs;
using Server.Data.Provider;
using Utils.Log;

namespace Client.MVVM.ViewModel
{
    public class StartupViewModel : ObservableObject
    {
        public string AppVersion => $"v{StringVersion.FullCallingAppVersion}";

        private string _status = "";

        public string StatusMessage { get { return _status; } set { _status = value; OnPropertyChanged(); } }

        private double _loadProgress;

        public double LoadProgress { get { return _loadProgress; } set { _loadProgress = value; OnPropertyChanged(); } }

        private int _loadingBarWidth;

        public int LoadingBarWidth
        {
            get { return _loadingBarWidth; }
            set { _loadingBarWidth = value; OnPropertyChanged(); }
        }

        private bool _showUpdateDialog;

        public bool ShowUpdateDialog
        {
            get { return _showUpdateDialog; }
            set { _showUpdateDialog = value; OnPropertyChanged(); }
        }

        private string _updateText = "An Update Is Available";

        public string UpdateText
        {
            get { return _updateText; }
            set { _updateText = value; OnPropertyChanged(); }
        }

        public StringVersion UpdateVersion = StringVersion.Zero;

        public StartupViewModel()
        {
            UpdateController.UpdateFound += (s, version) => {
                UpdateVersion = version;
                UpdateText = $"An Update Is Available: v{version}";
                ShowUpdateDialog = true;
            };

            ClientController.PreInitComplete += (s, e) => UpdateLoadProgress(LoadStatus.PreInit);
            ClientController.InitComplete += (s, e) => UpdateLoadProgress(LoadStatus.Init);
            ClientController.LateInitComplete += (s ,e) => UpdateLoadProgress(LoadStatus.PostInit);
            DataDragon.FileDownloadComplete += (s, e) => UpdateCacheDownloadProgress(e);
        }

        public void UpdateLoadProgress(LoadStatus loadStatus, double progress = 100)
        {
            double next = (double)loadStatus.Next();
            double conversion = (next - (double)loadStatus) / 100;
            LoadProgress = (double)loadStatus + (progress * conversion);
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is null)
                        return;
                    LoadingBarWidth = (int)(LoadProgress / 100 * Application.Current.MainWindow.ActualWidth);
                });
            }
            catch
            {
                //Cant update. Ignored
            }

        }

        public void UpdateCacheDownloadProgress(FileLoadProgressEventArgs e)
        {
            LoadProgress = (double)LoadStatus.CDragon + ((double)e.Completed / (double)e.Total * ((double)LoadStatus.CDragon.Next() - (double)LoadStatus.CDragon));
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (Application.Current.MainWindow is null)
                        return;
                    LoadingBarWidth = (int)(LoadProgress / 100 * Application.Current.MainWindow.ActualWidth);
                });
            }
            catch
            {
                //Cant update. Ignored
            }
            $"{e.Task} {e.Completed}/{e.Total} Assets: {e.FileName}".UpdateLoadStatus();
        }
    }
}
