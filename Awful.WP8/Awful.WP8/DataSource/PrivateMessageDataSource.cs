using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;

namespace Awful.Data
{
    public class PrivateMessageDataSource : Commands.BackgroundWorkerCommand<object>
    {
        private PrivateMessageMetadata _metadata;
        public virtual PrivateMessageMetadata Metadata
        {
            get { return _metadata; }
            set
            {
                SetProperty(ref _metadata, value, "Metadata");
                SetProperties(_metadata);
            }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value, "Title"); }
        }

        private string _subtitle;
        public string Subtitle
        {
            get { return _subtitle; }
            set { SetProperty(ref _subtitle, value, "Subtitle"); }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set { SetProperty(ref _description, value, "Description"); }
        }


        public void GetFormattedMessageAsync(Action<string> callback)
        {
            if (!this.IsBusy)
            {
                DoWorkArgs args = new DoWorkArgs()
                {
                    Source = this,
                    Callback = callback
                };

                this.Execute(args);
            }
        }

        private bool _isNew;
        public bool IsNew
        {
            get { return _isNew; }
            set { SetProperty(ref _isNew, value, "IsNew"); }
        }

        private string _postDate;
        public string PostDate
        {
            get { return _postDate; }
            set { SetProperty(ref _postDate, value, "PostDate"); }
        }

        public PrivateMessageDataSource() : base() { }

        private void SetProperties(PrivateMessageMetadata metadata)
        {
            this.Title = metadata.From;
            this.Subtitle = metadata.Subject;
            this.Description = GetBodySnippet(metadata.Body);
            this.IsNew = metadata.Status == PrivateMessageMetadata.MessageStatus.New;
            this.PostDate = GetFormattedPostDate(metadata.PostDate);
        }

        private string GetFormattedPostDate(DateTime? nullable)
        {
            string value = string.Empty;
            if (nullable.HasValue)
            {
                value = string.Format("{0} {1}",
                    nullable.Value.ToLongDateString(), nullable.Value.ToLongTimeString());
            }

            return value;
        }

        private string GetBodySnippet(string p)
        {
            if (string.IsNullOrEmpty(p))
                return p;

            // grab the first line
            var firstline = p.Substring(0, p.IndexOf(Environment.NewLine));
            return firstline;
        }

        class DoWorkArgs
        {
            public PrivateMessageDataSource Source { get; set; }
            public string Html { get; set; }
            public Action<string> Callback { get; set; }
        }

        protected override object DoWork(object parameter)
        {
            DoWorkArgs args = parameter as DoWorkArgs;
            args.Html = MetroStyler.Metrofy(args.Source.Metadata);
            return args;
        }

        protected override void OnError(Exception ex)
        {
            AwfulDebugger.AddLog(this, AwfulDebugger.Level.Critical, ex);
            MessageBox.Show("Could not load the message.", "Error", MessageBoxButton.OK);
        }

        protected override void OnCancel()
        {
            throw new NotImplementedException();
        }

        protected override void OnSuccess(object arg)
        {
            DoWorkArgs args = arg as DoWorkArgs;
            args.Callback(args.Html);
        }

        protected override bool PreCondition(object item)
        {
            return true;
        }
    }

    public class PrivateMessageDataGroup : PrivateMessageDataSource
    {
        public override PrivateMessageMetadata Metadata
        {
            get
            {
                return Items.First().Metadata;
            }
            set
            {
               
            }
        }

        public PrivateMessageDataGroup(IEnumerable<PrivateMessageDataSource> messages)
            :base()
        {
            if (messages.Count() == 0)
                throw new IndexOutOfRangeException("messages must not be empty");

            if (messages == null)
                throw new ArgumentNullException("messages must not be null");

            // first, sort messages by decending date
            var sorted = messages.OrderByDescending(new Func<PrivateMessageDataSource, long>(GetDateTime));

            // then add to items
            foreach (var item in sorted)
                this.Items.Add(item);

            // use top item as info base
            var first = Items.First();

            this.Title = first.Title;
            this.Subtitle = first.Subtitle;
            this.Description = FormatGroupDescription(Items);
        }

        private readonly ObservableCollection<PrivateMessageDataSource> _items = new ObservableCollection<PrivateMessageDataSource>();
        public ObservableCollection<PrivateMessageDataSource> Items
        {
            get { return _items; }
        }

        private string FormatGroupDescription(IEnumerable<PrivateMessageDataSource> messages)
        {
            int unread = messages.Count(message => { return message.IsNew; });
            int total = Items.Count;
            return string.Format("{0} {1}, {2} unread",
                total,
                total == 1 ? "message" : "messages",
                unread);
        }

        private long GetDateTime(PrivateMessageDataSource item)
        {
            return item.Metadata.PostDate.GetValueOrDefault().Ticks;
        }
    }
}
