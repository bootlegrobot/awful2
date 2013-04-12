using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Awful.Data;
using System.Windows;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Controls;

namespace Awful.ViewModels
{
    public class PrivateMessagesPageViewModel : ListViewModel<Data.PrivateMessageDataSource>
    {
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

        protected override void OnError(Exception exception)
        {
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
            IEnumerable<PrivateMessageMetadata> messages = null;

            // Is the folder list null? That means we need to grab all folders from the web.
            // The function always starts at the inbox, so we can grab the messages right now.
            if (this._folders == null)
            {
                var folders = PrivateMessageFolderDataSource.LoadUserFolders();
                this._folders = new List<PrivateMessageFolderDataSource>(folders);
                this._selectedFolder = this._folders[0];
                messages = this._selectedFolder.Metadata.Messages;
            }

            // get messages from selected folder and group up accordingly
            if (messages == null)
            {
                this._selectedFolder.Metadata = this._selectedFolder.Metadata.Refresh();
                messages = this._selectedFolder.Metadata.Messages;
            }

            return CondenseMessages(messages);
        }

        private IEnumerable<Data.PrivateMessageDataSource> CondenseMessages(IEnumerable<PrivateMessageMetadata> items)
        {
            var itemsAsList = items.ToList();
            var dataItems = new List<Data.PrivateMessageDataSource>(itemsAsList.Count);

            foreach (var item in items)
            {
                if (itemsAsList.Contains(item))
                {

                    // find all items that contain the same subject and author
                    string subject = item.Subject;
                    string author = item.From;
                    var conversation = items
                        .Where(message => message.Subject.Contains(subject) && message.From.Equals(author))
                        .ToArray();

                    // if there is more than one, then remove them from the list
                    if (conversation.Length > 1)
                    {
                        foreach (var found in conversation)
                            itemsAsList.Remove(found);

                        dataItems.Add(new Data.PrivateMessageDataGroup(conversation));
                    }

                    else
                    {
                        dataItems.Add(new Data.PrivateMessageDataSource() { Metadata = item });
                        itemsAsList.Remove(item);
                    }
                }
            }

            // sort items by postdate, decending
            return dataItems.OrderByDescending(data =>{ return data.Metadata.PostDate.GetValueOrDefault().Ticks; });
        }
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

    public class MessageGlyphSelector : IValueConverter
    {
        public UIElement ReplyGlyph { get; set; }
        public UIElement ForwardGlyph { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PrivateMessageMetadata)
            {
                try
                {
                    value = GetGlyph((PrivateMessageMetadata)value);
                }
                catch (Exception ex) { AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex); }
                
            }

            return value;
        }

        private UIElement GetGlyph(PrivateMessageMetadata metadata)
        {
            UIElement value = null;
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
                    value = new Grid();
                    break;
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
