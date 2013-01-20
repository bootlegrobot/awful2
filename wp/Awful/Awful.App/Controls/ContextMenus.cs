using Awful.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Telerik.Windows.Controls;
using System.Windows.Input;

namespace Awful.Controls
{
    public class ForumContextMenu : RadContextMenu
    {
        private const string PIN = "pin this forum";
        private const string UNPIN = "unpin this forum";

        private readonly RadContextMenuItem _togglePinned;

        public ForumContextMenu()
            : base()
        {
            _togglePinned = new RadContextMenuItem();
            _togglePinned.Tap += TogglePinned_Tap;

            this.Items.Add(_togglePinned);
            this.Opening += ForumContextMenu_Opening;
        }

        private void TogglePinned_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var context = (sender as FrameworkElement).DataContext;
            var forum = context as ForumDataSource;
            
            forum.IsPinned = !forum.IsPinned;
            PinnedItemsManager.NotifyObservers(forum);
        }

        private void ForumContextMenu_Opening(object sender, ContextMenuOpeningEventArgs e)
        {
            var context = (e.FocusedElement as FrameworkElement).DataContext;
            bool isPinned = (context as ForumDataSource).IsPinned;

            _togglePinned.Content = isPinned ? UNPIN : PIN;
        }
    }

    public class PinnedContextMenu : RadContextMenu
    {
        private const string UNPIN = "unpin this item";
        private readonly RadContextMenuItem _unpinMenu;

        public PinnedContextMenu()
            : base()
        {
            _unpinMenu = new RadContextMenuItem();
            _unpinMenu.Content = UNPIN;
            _unpinMenu.Tap += UnpinMenu_Tap;
        }

        private void UnpinMenu_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var context = sender as FrameworkElement;
            var pin = context as IPinnable;
            pin.IsPinned = false;

            PinnedItemsManager.NotifyObservers(pin);
        }
    }

    public class ThreadContextMenu : RadContextMenu
    {
        private const string MARK = "add to bookmarks";
        private const string UNMARK = "remove from bookmarks";
        private const string JUMP = "jump to page";
        private const string CLEAR = "clear marked posts";

        private readonly RadContextMenuItem _toggleBookmarks;
        private readonly RadContextMenuItem _jumpToPage;
        private readonly RadContextMenuItem _clearMarkedPosts;

        public event EventHandler JumpToPageRequest;

        public ThreadContextMenu()
        {
            this._toggleBookmarks = new RadContextMenuItem();
            this._toggleBookmarks.Command = new Commands.ToggleBookmarkCommand();

            this._jumpToPage = new RadContextMenuItem();
            this._jumpToPage.Content = JUMP;
            this._jumpToPage.Tapped += new EventHandler<ContextMenuItemSelectedEventArgs>(OnJumpToPageTapped);

            this._clearMarkedPosts = new RadContextMenuItem();
            this._clearMarkedPosts.Command = new Commands.ClearMarkedPostsCommand();
            this._clearMarkedPosts.Content = CLEAR;

            this.Items.Add(_toggleBookmarks);
            this.Items.Add(_jumpToPage);
            this.Items.Add(_clearMarkedPosts);
            this.Opening += new EventHandler<ContextMenuOpeningEventArgs>(OnMenuOpening);
        }

        void OnJumpToPageTapped(object sender, ContextMenuItemSelectedEventArgs e)
        {
            var context = this.DataContext as ThreadDataSource;
            OnJumpToPageRequest(context);
        }

        void OnMenuOpening(object sender, ContextMenuOpeningEventArgs e)
        {
            var context = (e.FocusedElement as FrameworkElement).DataContext;
            var thread = context as ThreadDataSource;
            bool isBookmarked = MainDataSource.Instance.Bookmarks.Contains(thread);

            _toggleBookmarks.CommandParameter = thread;
            _toggleBookmarks.Content = isBookmarked ? UNMARK : MARK;
            _clearMarkedPosts.CommandParameter = thread;
        }

        private void OnJumpToPageRequest(Data.ThreadDataSource thread)
        {
            if (JumpToPageRequest != null)
                JumpToPageRequest(thread, EventArgs.Empty);
        }
    }
}
