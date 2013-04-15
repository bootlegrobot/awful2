using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Awful.Data;
using System.ComponentModel;

namespace Awful.ViewModels
{
    public class PrivateMessageDetailsViewModel : Common.BindableBase
    {

        public PrivateMessageDetailsViewModel()
            : base()
        {
            if (DesignerProperties.IsInDesignTool)
            {
                _currentFolder = new PrivateMessageFolderDataSource()
                {
                    Title = "Sample Folder",
                    Subtitle = "1 of 9 messages, 0 unread"
                };

                _selectedItem = new PrivateMessageDataSource()
                {
                    Title = "H2SO4",
                    Subtitle = "Re: Awful Beta Invite",
                    Description = "4/13/2013 8:00 PM"
                };
            }
        }

        private bool _showWebView = false;
        public bool ShowWebView
        {
            get { return _showWebView; }
            set { SetProperty(ref _showWebView, value, "ShowWebView"); }
        }

        public bool HasPrev { get { return _selectedIndex > 0; } }
        public bool HasNext { get { return _selectedIndex < _items.Count; } }

        private PrivateMessageFolderDataSource _currentFolder;
        public PrivateMessageFolderDataSource CurrentFolder
        {
            get { return _currentFolder; }
            set { SetProperty(ref _currentFolder, value, "CurrentFolder"); }
        }

        private PrivateMessageDataSource _selectedItem;
        public PrivateMessageDataSource SelectedItem
        {
            get { return _selectedItem; }
            set
            {
                if (SetProperty(ref _selectedItem, value, "SelectedItem"))
                {
                    this._selectedIndex = Items.IndexOf(value);
                    this._currentFolder.UpdateSubtitle(this._selectedIndex);
                }
            }
        }

        private int _selectedIndex;

        private IList<PrivateMessageDataSource> _items;
        public IList<PrivateMessageDataSource> Items
        {
            get { return _items; }
            set { SetProperty(ref _items, value, "Items"); }
        }

        public bool ShowNextItem()
        {
            int index = _selectedIndex + 1;
            int count = Items.Count;
            if (index < count)
                SelectedItem = Items[index];

            return index < count;
        }

        public bool ShowPrevItem()
        {
            int index = _selectedIndex - 1;
            if (index > -1)
                SelectedItem = Items[index];

            return index > -1;
        }
    }
}
