using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Awful
{
    public abstract class PrivateMessageRequestStrategy
    {
        public abstract PrivateMessageMetadata LoadMessage(string privateMessageId);
        public abstract IPrivateMessageRequest ForwardMessage(string privateMessageId);
        public abstract IPrivateMessageRequest BeginReplyToMessage(string privateMessageId);
        public abstract bool SendMessageRequest(IPrivateMessageRequest request);
        public abstract bool DeleteMessage(string folderId, string privateMessageId);
        public abstract bool MoveMessage(string fromFolderId, string toFolderId, string privateMessageId);
        public abstract bool MoveMessages(string fromFolderId, string toFolderId, IEnumerable<string> privateMessageIds);

        public abstract PrivateMessageFolderMetadata LoadFolder(string folderId);
        public abstract bool CreateNewFolder(string name);
        public abstract bool RenameFolder(string folderId, string name);
        public abstract bool DeleteFolder(string folderId);
        public abstract bool DeleteAllMessages(string folderId, IEnumerable<string> privateMessageIds);
    }
}
