using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using ChromeTabs;
using Client.MVVM.Core;
using Client.MVVM.ViewModel;
using Utils;
using Utils.Log;

namespace Demo.ViewModel
{
    public class ViewModelBase : ObservableObject
    {
        //since we don't know what kind of objects are bound, so the sorting happens outside with the ReorderTabsCommand.
        public RelayCommand<TabReorder> ReorderTabsCommand { get; set; }
        public RelayCommand AddTabCommand { get; set; }
        public RelayCommand<TabBase> CloseTabCommand { get; set; }
        public ObservableCollection<TabBase> ItemCollection { get; set; }

        //This is the current selected tab, if you change it, the tab is selected in the tab control.
        private TabBase _selectedTab;
        public TabBase SelectedTab
        {
            get => _selectedTab;
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _canAddTabs;
        public bool CanAddTabs
        {
            get => _canAddTabs;
            set
            {
                if (_canAddTabs == value) return;
                _canAddTabs = value;
                OnPropertyChanged();
                AddTabCommand.RaiseCanExecuteChanged();
            }
        }


        public ViewModelBase()
        {
            ItemCollection = new ObservableCollection<TabBase>();
            ItemCollection.CollectionChanged += ItemCollection_CollectionChanged;
            ReorderTabsCommand = new RelayCommand<TabReorder>(ReorderTabsCommandAction);
            AddTabCommand = new RelayCommand(AddTabCommandAction, CanAddTabs);
            CloseTabCommand = new RelayCommand<TabBase>(CloseTabCommandAction);
            CanAddTabs = true;

            ItemCollection.Add(CreateIngameTab());
        }

        protected static IngameControlViewModel CreateIngameTab()
        {
            return new IngameControlViewModel { TabName = "Ingame", TestString = "Ingame Test String" };
        }

        protected static PreGameControlViewModel CreatePreGameTab()
        {
            return new PreGameControlViewModel { TabName = "Pre Game"};
        }

        protected static PostGameControlViewModel CreatePostGameTab()
        {
            return new PostGameControlViewModel { TabName = "Post Game" };
        }

        /// <summary>
        /// Reorder the tabs and refresh collection sorting.
        /// </summary>
        /// <param name="reorder"></param>
        protected virtual void ReorderTabsCommandAction(TabReorder reorder)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(ItemCollection);
            int from = reorder.FromIndex;
            int to = reorder.ToIndex;
            var tabCollection = view.Cast<TabBase>().ToList();//Get the ordered collection of our tab control

            tabCollection[from].TabNumber = tabCollection[to].TabNumber; //Set the new index of our dragged tab

            if (to > from)
            {
                for (int i = from + 1; i <= to; i++)
                {
                    tabCollection[i].TabNumber--; //When we increment the tab index, we need to decrement all other tabs.
                }
            }
            else if (from > to)//when we decrement the tab index
            {
                for (int i = to; i < from; i++)
                {
                    tabCollection[i].TabNumber++;//When we decrement the tab index, we need to increment all other tabs.
                }
            }

            view.Refresh();//Refresh the view to force the sort description to do its work.
        }

        //We need to set the TabNumber property on the viewmodels when the item source changes to keep it in sync.
        private void ItemCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (TabBase tab in e.NewItems)
                {
                    if (ItemCollection.Count > 1)
                    {
                        //If the new tab don't have an existing number, we increment one to add it to the end.
                        if (tab.TabNumber == 0)
                            tab.TabNumber = ItemCollection.OrderBy(x => x.TabNumber).LastOrDefault().TabNumber + 1;
                    }
                }
            }
            else
            {
                List<TabBase> tabCollection = ItemCollection.ToList();
                foreach (TabBase item in tabCollection)
                    item.TabNumber = tabCollection.IndexOf(item);
            }
        }

        //To close a tab, we simply remove the viewmodel from the source collection.
        private void CloseTabCommandAction(TabBase vm)
        {
            int count = ItemCollection.Count;
            bool removed = ItemCollection.Remove(vm);
            if(!removed || count - 1 != ItemCollection.Count)
            {
                "Something went wrong removing tab from collection".Warn();
            }
        }

        private void AddTabCommandAction()
        {
            TabBase toAdd;


            Random r = new();
            int num = r.Next(1, 100);
            toAdd = num < 33 ? CreateIngameTab() : num < 66 ? CreatePreGameTab() : CreatePostGameTab();

            ItemCollection.Add(toAdd);

            /*
            if (!ItemCollection.Any(t => t.GetType() == toAdd.GetType()))
            {
                ItemCollection.Add(toAdd);
            }
            */
        }
    }
}