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

        private void SetProperties(PrivateMessageFolderMetadata value)
        {
            this.Title = value.Name;
        }

        public static IEnumerable<PrivateMessageFolderDataSource> LoadUserFolders()
        {
            var folders = MainDataSource.Instance.CurrentUser.Metadata.LoadPrivateMessageFolders();
            return folders.Select(folder => new PrivateMessageFolderDataSource() { Metadata = folder });
        }
    }
}
