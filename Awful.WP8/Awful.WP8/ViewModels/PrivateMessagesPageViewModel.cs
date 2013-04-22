using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awful.Data;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;
using System.Collections.ObjectModel;

namespace Awful.ViewModels
{
    public class PrivateMessagesPageViewModel : ListViewModel<Data.PrivateMessageDataSource>
    {
        public PrivateMessagesPageViewModel()
            : base()
        {
            Items.CollectionChanged += Items_CollectionChanged;
        }

        void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // TODO: Implement Add, Delete Sync with server
        }

        private bool IgnoreCollectionChanged { get; set; }

        private IList<PrivateMessageDataSource> _unreadItems;
        public IList<PrivateMessageDataSource> UnreadItems
        {
            get { return _unreadItems; }
            set { SetProperty(ref _unreadItems, value, "UnreadItems"); }
        }

        private List<PrivateMessageFolderDataSource> _folders = null;
        public List<PrivateMessageFolderDataSource> Folders
        {
            get { return _folders; }
            set { SetProperty(ref _folders, value, "Folders"); }
        }

        private PrivateMessageFolderDataSource _selectedFolder = null;
        public PrivateMessageFolderDataSource SelectedFolder
        {
            get { return _selectedFolder; }
            set
            {
                if (SetProperty(ref _selectedFolder, value, "SelectedFolder"))
                {
                    this.LoadData();
                }
            }
        }

        #region SelectMode

        private bool _isSelectModeActive = false;
        public bool IsSelectModeActive
        {
            get { return _isSelectModeActive; }
            set
            {
                if (SetProperty(ref _isSelectModeActive, value))
                    OnSelectModeChanged(this);
            }
        }

        private event EventHandler SelectModeChanged;

        private void OnSelectModeChanged(PrivateMessagesPageViewModel privateMessagesPageViewModel)
        {
            var handler = SelectModeChanged;
            if (handler != null)
                handler(privateMessagesPageViewModel, EventArgs.Empty);
        }

       
        #endregion

        protected override void OnError(Exception exception)
        {
            IsRunning = false;
            Items.Clear();
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, exception);
            MessageBox.Show("Could not fetch private messages from the server.", "Error", MessageBoxButton.OK);
        }

        protected override void OnCancel()
        {
           
        }

        protected override void OnSuccess()
        {
            // notify ui of property changes
            OnPropertyChanged("Folders");
            OnPropertyChanged("SelectedFolder");
        }

        protected override IEnumerable<Data.PrivateMessageDataSource> LoadDataInBackground()
        {
            IEnumerable<Data.PrivateMessageDataSource> messages = null;

            // Is the folder list null? That means we need to grab all folders from the web.
            if (this._folders == null)
            {
                var folders = PrivateMessageFolderDataSource.LoadUserFolders();
                this._folders = new List<PrivateMessageFolderDataSource>(folders);
                this._selectedFolder = this._folders[0];
            }
                
            messages = this._selectedFolder.GetMessages();

            // filter unread items
            try
            {
                var unread = messages.Where(m => m.Metadata.Status == PrivateMessageMetadata.MessageStatus.New);
                unread = CondenseMessages(unread);
                List<Data.PrivateMessageDataSource> unreadList = new List<PrivateMessageDataSource>(unread);
                this._unreadItems = unreadList;
            }
            catch (Exception ex) { }

            return CondenseMessages(messages);
        }

        private IEnumerable<Data.PrivateMessageDataSource> CondenseMessages(IEnumerable<Data.PrivateMessageDataSource> items)
        {
            var itemsAsList = items.ToList();
            var condensedItems = new List<Data.PrivateMessageDataSource>(itemsAsList.Count);

            foreach (var item in items)
            {
                if (itemsAsList.Contains(item))
                {
                    // find all items that contain the same subject and author
                    string subject = item.Metadata.Subject.ToLower();
                    string author = item.Metadata.From;
                    var conversation = items
                        .Where(message => message.Metadata.Subject.ToLower().Contains(subject) && 
                            message.Metadata.From.Equals(author))
                        .ToArray();

                    // if there is more than one, then remove them from the list
                    if (conversation.Length > 1)
                    {
                        foreach (var found in conversation)
                            itemsAsList.Remove(found);

                        condensedItems.Add(new Data.PrivateMessageDataGroup(conversation));
                    }

                    else
                    {
                        condensedItems.Add(item);
                        itemsAsList.Remove(item);
                    }
                }
            }

            // sort items by postdate, decending
            return condensedItems.OrderByDescending(data =>{ return data.Metadata.PostDate.GetValueOrDefault().Ticks; });
        }

        public PrivateMessageDataSource SelectedItem { get; set; }
    }

    public class PrivateMessageTemplateSelector : Telerik.Windows.Controls.DataTemplateSelector
    {
        public DataTemplate PrivateMessageTemplate { get; set; }
        public DataTemplate PrivateMessageGroupTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PrivateMessageDataGroup)
                return PrivateMessageGroupTemplate;

            else if (item is PrivateMessageDataSource)
                return PrivateMessageTemplate;


            return base.SelectTemplate(item, container);
        }
    }

    public class MessageGlyphSelector : Controls.DataTemplateSelector
    {
        public DataTemplate ReplyGlyph { get; set; }
        public DataTemplate ForwardGlyph { get; set; }
        public DataTemplate EmptyGlyph { get; set; }

        protected override DataTemplate SelectTemplate(object newContent, DependencyObject container)
        {
            var message = newContent as PrivateMessageMetadata;
            return GetGlyph(message);
        }

        private DataTemplate GetGlyph(PrivateMessageMetadata metadata)
        {
            DataTemplate value = null;
            var status = metadata.Status;
            switch (status)
            {
                case PrivateMessageMetadata.MessageStatus.Forwarded:
                    value = ForwardGlyph;
                    break;

                case PrivateMessageMetadata.MessageStatus.Replied:
                    value = ReplyGlyph;
                    break;

                default:
                    value = EmptyGlyph;
                    break;
            }

            return value;
        }
    }
}
