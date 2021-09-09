using ChromeTabs;
using Client.MVVM.Controls;
using Client.MVVM.ViewModel;
using Demo.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Media3D;

namespace Client.MVVM.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : WindowBase
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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

        /// <summary>
        /// This event triggers when a tab is dragged outside the bonds of the tab control panel.
        /// We can use it to create a docking tab control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabControl_TabDraggedOutsideBonds(object sender, TabDragEventArgs e)
        {
            TabBase draggedTab = e.Tab as TabBase;
            if (TryDragTabToWindow(e.CursorPosition, draggedTab))
            {
                //Set Handled to true to tell the tab control that we have dragged the tab to a window, and the tab should be closed.
                e.Handled = true;
            }
        }

        protected override bool TryDockWindow(Point position, TabBase dockedWindowVM)
        {
            //Hit test against the tab control
            if (MainTabControl.InputHitTest(position) is FrameworkElement element)
            {
                ////test if the mouse is over the tab panel or a tab item.
                if (CanInsertTabItem(element))
                {
                    //TabBase dockedWindowVM = (TabBase)win.DataContext;
                    ViewModelBase vm = (ViewModelBase)DataContext;
                    vm.ItemCollection.Add(dockedWindowVM);
                    vm.SelectedTab = dockedWindowVM;
                    //We run this method on the tab control for it to grab the tab and position it at the mouse, ready to move again.
                    MainTabControl.GrabTab(dockedWindowVM);
                    return true;
                }
            }
            return false;
        }

        private void OpenSettingsWindow_Click(object sender, RoutedEventArgs e)
        {
            EnableDarkenContent();
        }

        public void EnableDarkenContent()
        {
            DarkenContentRect.Visibility = Visibility.Visible;
        }

        public void DisableDarkenContent()
        {
            DarkenContentRect.Visibility = Visibility.Hidden;
        }
    }
}
