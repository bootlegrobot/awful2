using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Awful.Data;

namespace Awful
{
    public static class PinnedItemsManager
    {
        public static event EventHandler<PinnedItemEventArgs> PinnedStatusChanged;

        private static void OnPinnedStatusChanged(IPinnable item)
        {
            if (PinnedStatusChanged != null)
                PinnedStatusChanged(item, new PinnedItemEventArgs(item));
        }

        public static void NotifyObservers(IPinnable item)
        {
            OnPinnedStatusChanged(item);
        }
    }

    public sealed class PinnedItemEventArgs : EventArgs
    {
        public IPinnable Item { get; private set; }
        public PinnedItemEventArgs(IPinnable item) { Item = item; }
    }
}
