using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace Awful.Controls
{
    public partial class ThreadReplyControl : UserControl
    {
        public ThreadReplyControl()
        {
            InitializeComponent();
            Commands.EditPostCommand.EditRequested += OnEditRequestRecieved;
        }

        private void OnEditRequestRecieved(object sender, Common.ThreadPostRequestEventArgs args)
        {
            this.ReplyViewModel.Request = args.Request;
        }

        public delegate void ButtonClickDelgate(object sender, System.EventArgs e);

        public ButtonClickDelgate SaveDraftClick { get; set; }
        public ButtonClickDelgate ReplyClick { get; set; }
        public ButtonClickDelgate CancelEditClick { get; set; }

        private ViewModels.SmileyListViewModel SmileyViewModel
        {
            get { return this.Resources["smileyViewModel"] as ViewModels.SmileyListViewModel; }
        }

        public ViewModels.ReplyViewModel ReplyViewModel
        {
            get { return this.Resources["replyViewModel"] as ViewModels.ReplyViewModel; }
        }

        public IApplicationBar ReplyAppBar
        {
            get { return this.Resources["ReplyAppBar"] as IApplicationBar; }
        }

        private void ClearAllReplyText(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            this.ReplyTextBox.Text = string.Empty;
        }

        private void CancelEditRequest(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            if (CancelEditClick == null)
                throw new NullReferenceException("CancelEditRequest must not be null!");

            CancelEditClick(sender, e);
        }

        private void SaveReplyDraft(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            if (SaveDraftClick == null)
                throw new NullReferenceException("SaveDraftDelegate must not be null!");

            SaveDraftClick(sender, e);
        }

        private void ShowTags(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
        }

        private void ShowSmilies(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
        }

        private void SendReply(object sender, System.EventArgs e)
        {
            // TODO: Add event handler implementation here.
            if (ReplyClick == null)
                throw new NullReferenceException("ReplyDelgate must be set!");

            ReplyClick(sender, e);
        }

		private void AppendTagToReplyText(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
        {
           // TODO: Add event handler implementation here.
            var item = e.Item.AssociatedDataItem.Value as Data.CodeTagDataModel;
            
            // set clipboard text to title text
            Clipboard.SetText(item.Code);

            // move back to reply box
            this.LayoutRoot.SelectedIndex = 0;
        }

		private void LoadMoreSmilies(object sender, System.Windows.Input.GestureEventArgs e)
		{
			// TODO: Add event handler implementation here.
            SmileyViewModel.AppendNextPage();
		}

		private void AppendSmilieyToReplyText(object sender, Telerik.Windows.Controls.ListBoxItemTapEventArgs e)
		{
			// TODO: Add event handler implementation here.
            // TODO: Add event handler implementation here.
            var item = e.Item.AssociatedDataItem.Value as Data.SmilieyDataModel;

            // set clipboard text to title text
            Clipboard.SetText(item.Code);

            // move back to reply box
            this.LayoutRoot.SelectedIndex = 0;
		}

		private void SmileyDataRequested(object sender, System.EventArgs e)
		{
			// TODO: Add event handler implementation here.
            var listBox = sender as Telerik.Windows.Controls.RadDataBoundListBox;
            listBox.DataVirtualizationMode = Telerik.Windows.Controls.DataVirtualizationMode.OnDemandManual;
            SmileyViewModel.AppendNextPage();
		}

        private void UpdateTextCount(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            
        }

        private void ScrollReplyBoxToBottom(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            ReplyTextBox.UpdateLayout();
            replyScroll.ScrollToVerticalOffset(ReplyTextBox.ActualHeight);
        }

    }
}
