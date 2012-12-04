using Awful.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Telerik.Windows.Controls;

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

}
