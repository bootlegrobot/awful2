using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Awful
{
    public interface IPrivateMessageRequest
    {
        TagMetadata SelectedTag { get; set; }
        string PrivateMessageId { get; }
        string To { get; set; }
        string Subject { get; set; }
        string FormKey { get; }
        string FormCookie { get; }
        bool IsForward { get; }
        string Body { get; set; }
        bool SendMessage();
    }

    [DataContract]
    public class PrivateMessageRequest : IPrivateMessageRequest
    {
        [DataMember]
        public string PrivateMessageId { get; set; }
        [DataMember]
        public TagMetadata SelectedTag { get; set; }
        [DataMember]
        public string FormKey { get; set; }
        [DataMember]
        public string FormCookie { get; set; }
        [DataMember]
        public bool IsForward { get; set; }
        [DataMember]
        public string To { get; set; }
        [DataMember]
        public string Subject { get; set; }
        [DataMember]
        public string Body { get; set; }

        public bool SendMessage()
        {
            return false;
        }
    }
}
