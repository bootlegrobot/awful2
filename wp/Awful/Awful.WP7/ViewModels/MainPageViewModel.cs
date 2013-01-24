using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Awful.ViewModels
{
    public class MainPageViewModel : Data.CommonDataGroup
    {
        private NavigationService NavigationService;

        public MainPageViewModel(NavigationService service)
            : base()
        {
            this.NavigationService = service;

            Items.Add(
                new MainPageItem()
                {
                    Title = "Forums",
                    Description = "The Something Awful pinnedForums.",
                    Command = new Common.ActionCommand(NavigateToForumListPage),
                    CommandParameter = NavigationService
                });

            Items.Add(
                new MainPageItem()
                {
                    Title = "Pins",
                    Description = "Browse pinned pinnedForums and bookmarks.",
                    Command = new Common.ActionCommand(NavigateToPinnedItemsPage),
                    CommandParameter = NavigationService
                });
        }

        private void NavigateToForumListPage(object service)
        {
            (service as NavigationService).Navigate(new Uri("/ForumListPage.xaml", UriKind.RelativeOrAbsolute));
        }

        private void NavigateToPinnedItemsPage(object service)
        {
            (service as NavigationService).Navigate(new Uri("/PinnedItemsPage.xaml", UriKind.RelativeOrAbsolute));
        }
    }

    public class MainPageItem : Data.CommonDataItem
    {
        public MainPageItem() : base() { }

        private ICommand _command;
        public ICommand Command
        {
            get { return _command; }
            set { SetProperty(ref _command, value, "Command"); }
        }

        private object _commandParameter;
        public object CommandParameter
        {
            get { return this._commandParameter; }
            set { this.SetProperty(ref _commandParameter, value, "CommandParameter"); }
        }
    }
}
