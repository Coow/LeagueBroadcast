using Client.Utils;
using Common;
using Demo.ViewModel;
using System.Collections.ObjectModel;
using System.Windows;
using Utils;
using Utils.Log;

namespace Client.MVVM.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        public static MainViewModel? Instance { get; set; }
        public static string AppVersion => $"v{StringVersion.CallingAppVersion}";

        private string _status = "";

        public Window? SettingsWindow { get; set; }
        public SettingsViewModel? SettingsCtx { get; set; }

        private ClientConnectionStatus _connectionStatus = ClientConnectionStatus.DISCONNECTED;

        public ClientConnectionStatus ConnectionStatus
        {
            get { return _connectionStatus; }
            set { _connectionStatus = value; OnPropertyChanged(); "Connection State Changed".Info(); }
        }


        private bool _canMoveTabs;
        public bool CanMoveTabs
        {
            get => _canMoveTabs;
            set
            {
                if (_canMoveTabs != value)
                {
                    _canMoveTabs = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showAddButton;
        public bool ShowAddButton
        {
            get => _showAddButton;
            set
            {
                if (_showAddButton != value)
                {
                    _showAddButton = value;
                    OnPropertyChanged();
                }
            }
        }

        public string StatusMessage { get => _status; set { _status = value; OnPropertyChanged(); } }

        public MainViewModel()
        {
            if (Instance is null)
            {
                Instance = this;
            }

            CanAddTabs = true;
            CanMoveTabs = true;
            ShowAddButton = true;
            ConnectionStatus = ClientConnectionStatus.ConnectionStatusMap[AppStatus.CurrentStatus];

            AppStatus.StatusUpdate += (s, e) =>
            {
                ConnectionStatus = ClientConnectionStatus.ConnectionStatusMap[e];
            };
        }
    }
}
