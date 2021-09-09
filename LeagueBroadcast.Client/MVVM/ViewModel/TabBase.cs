using System;
using System.Windows.Media;
using Utils;

namespace Client.MVVM.ViewModel
{
    public abstract class TabBase : ObservableObject
    {
        private int _tabNumber;
        public int TabNumber
        {
            get => _tabNumber;
            set
            {
                if (_tabNumber != value)
                {
                    _tabNumber = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _tabName = "";
        public string TabName
        {
            get => _tabName;
            set
            {
                if (_tabName != value)
                {
                    _tabName = value;
                    OnPropertyChanged();
                }
            }
        }


        private bool _isPinned;
        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned != value)
                {
                    _isPinned = value;
                    OnPropertyChanged();
                }
            }
        }


        private ImageSource _tabIcon;
        public ImageSource TabIcon
        {
            get => _tabIcon;
            set
            {
                if (!Equals(_tabIcon, value))
                {
                    _tabIcon = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
