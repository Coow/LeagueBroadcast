using Client.MVVM.ViewModel;
using Client.Utils;
using Common;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Client.MVVM.Controls
{
    /// <summary>
    /// Interaction logic for DockingWindow.xaml
    /// </summary>
    public partial class DockingWindow : Window
    {
        public DockingWindow()
        {
            InitializeComponent();
            AppStatus.StatusUpdate += UpdateConnectionState;
            ConnectionStatus.DataContext = MainViewModel.Instance!.ConnectionStatus;
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            //Redock this control when closed instead of actually closing it... Do this for now until I find a better way to readd them
            if (!ClientController.MainCtx?.ItemCollection.Any(tab => tab.GetType() == DataContext.GetType()) ?? true)
            {
                ClientController.MainCtx?.ItemCollection.Add((TabBase)DataContext);
            }
            AppStatus.StatusUpdate -= UpdateConnectionState;
            Close();
        }

        private void UpdateConnectionState(object? sender, ConnectionStatus e)
        {
            ConnectionStatus.DataContext = ClientConnectionStatus.ConnectionStatusMap[e];
        }

        private void MinimizeApp_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void AppState_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState != WindowState.Maximized ? WindowState.Maximized : WindowState.Normal;
        }

        private void HeaderBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Storyboard sb = Resources["FadeInContentAnim"] as Storyboard;
            sb?.Begin();
        }

        public void SetWindowTitle(string Title)
        {
            WindowTitle.Content = Title;
        }
    }
}
