using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Awful.Data
{
    public class PrivateMessageFolderDataSource : CommonDataItem
    {
        private PrivateMessageFolderMetadata _metadata;
        public PrivateMessageFolderMetadata Metadata
        {
            get { return _metadata; }
            set
            {
                SetProperty(ref _metadata, value, "Metadata");
                SetProperties(value);
            }
        }

        private List<PrivateMessageDataSource> _messages;

        public List<PrivateMessageDataSource> GetMessages()
        {
            if (_messages == null)
                Refresh();

            return _messages;
        }

        public void Refresh()
        {
            if (this._metadata == null)
                throw new Exception("Cannot refresh with null metadata.");

            var refreshed = this._metadata.Refresh();
            Metadata = refreshed;
        }
      
        private void SetProperties(PrivateMessageFolderMetadata value)
        {
            this.Title = value.Name;
            if (!value.Messages.IsNullOrEmpty())
                this._messages = value.Messages
                    .Select(message => new PrivateMessageDataSource() { Metadata = message })
                    .ToList();
        }

        public static IEnumerable<PrivateMessageFolderDataSource> LoadUserFolders()
        {
            var folders = MainDataSource.Instance.CurrentUser.Metadata.LoadPrivateMessageFolders();
            return folders.Select(folder => new PrivateMessageFolderDataSource() { Metadata = folder });
        }

        internal void UpdateSubtitle(int p)
        {
            int count = _messages.Count;
            int index = p + 1;
            this.Subtitle = string.Format("{0} of {1} messages", index, count);
        }
    }
}
