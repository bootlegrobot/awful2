using System;
using System.Linq;
using System.Collections.Generic;

namespace Awful.Common
{
    public sealed class PrivateMessageHttpRequest : PrivateMessageRequestStrategy
    {

        public override PrivateMessageMetadata LoadMessage(string privateMessageId)
        {
            int id = -1;
            if (int.TryParse(privateMessageId, out id))
                return PrivateMessageService.Service.FetchMessage(id);
            else
                return null;
        }

        public override IPrivateMessageRequest ForwardMessage(string privateMessageId)
        {
            IPrivateMessageRequest request = null;

            int id = -1;
            if (int.TryParse(privateMessageId, out id))
                request = PrivateMessageService.Service.BeginForwardToMessageRequest(id);
            return request;
        }

        public override IPrivateMessageRequest BeginReplyToMessage(string privateMessageId)
        {
            IPrivateMessageRequest request = null;
            int id = -1;
            if (int.TryParse(privateMessageId, out id))
                request = PrivateMessageService.Service.BeginReplyToMessageRequest(id);
            return request;
        }

        public override bool SendMessageRequest(IPrivateMessageRequest request)
        {
            return PrivateMessageService.Service.SendMessage(request);
        }

        public override bool DeleteMessage(string folderId, string privateMessageId)
        {
            bool success = false;
            int id = -1;
            int folder = -1;
            if (int.TryParse(privateMessageId, out id) &&
                int.TryParse(folderId, out folder))
            {
                success = PrivateMessageService.Service.DeleteMessage(id, folder, folder);
            }

            return success;
        }

        public override bool MoveMessage(string fromFolderId, string toFolderId, string privateMessageId)
        {
            int id = int.Parse(privateMessageId);
            int srcId = int.Parse(fromFolderId);
            int destId = int.Parse(toFolderId);
            return PrivateMessageService.Service.MoveMessage(id, srcId, destId);
        }

        public override bool MoveMessages(string fromFolderId, string toFolderId, IEnumerable<string> privateMessageIds)
        {
            int srcId = int.Parse(fromFolderId);
            int destId = int.Parse(toFolderId);

            return PrivateMessageService.Service.MoveMessages(
                privateMessageIds.Select(message => int.Parse(message)),
                srcId, destId);
        }

        public override PrivateMessageFolderMetadata LoadFolder(string folderId)
        {
            int id = -1;
            PrivateMessageFolderMetadata folder = null;
            if (int.TryParse(folderId, out id))
                folder = PrivateMessageService.Service.FetchFolder(id);

            return folder;
        }

        public override bool CreateNewFolder(string name)
        {
            if (name == null || name == string.Empty)
                throw new Exception("Cannot create a new folder with no name!");

            bool success = false;
            var editor = PrivateMessageService.Service.BeginEditFolderRequest();
            editor.CreateFolder(name);
            success = editor.SendRequest();

            return success;
        }

        public override bool RenameFolder(string folderId, string name)
        {
            bool success = false;

            var editor = PrivateMessageService.Service.BeginEditFolderRequest();
            editor.RenameFolder(new PrivateMessageFolderMetadata() { FolderId = folderId }, name);
            success = editor.SendRequest();
            return success;
        }

        public override bool DeleteFolder(string folderId)
        {
            bool success = false;
            var editor = PrivateMessageService.Service.BeginEditFolderRequest();
            editor.DeleteFolder(new PrivateMessageFolderMetadata() { FolderId = folderId });
            success = editor.SendRequest();
            return success;
        }

        public override bool DeleteAllMessages(string folderId, IEnumerable<string> privateMessageIds)
        {
            int folder = int.Parse(folderId);
            var messageIds = privateMessageIds.Select(message => int.Parse(message));
            return PrivateMessageService.Service.DeleteMessages(messageIds, folder, folder);
        }
    }
}