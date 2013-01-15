using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Awful.Common
{
    [CollectionDataContract]
    public abstract class ObservableSet<T> : ObservableCollection<T>, IEqualityComparer<T>
    {
        private DateTime? _lastUpdated = null;
        [DataMember]
        public DateTime? LastUpdated
        {
            get { return _lastUpdated; }
            set { _lastUpdated = value; }
        }

        public ObservableSet()
            : base()
        {
           
        }

        public ObservableSet(IEnumerable<T> items)
            : base(items)
        {
          
        }

        public virtual bool Equals(T x, T y)
        {
            return GetHashCode(x).Equals(GetHashCode(y));
        }

        public virtual int GetHashCode(T obj)
        {
            return obj.GetHashCode();
        }

        private void Update(IEnumerable items)
        {
            foreach (var item in items)
                AddOrUpdate((T)item);
        }

        public bool ReplaceItem(T newItem)
        {
            var oldItem = this.Where(item => this.Equals(newItem, item)).SingleOrDefault();
            if (oldItem != null)
            {
                int index = IndexOf(oldItem);
                RemoveItem(index);
                InsertItem(index, newItem);
            }

            return oldItem != null;
        }

        protected void AddOrUpdate(T newItem)
        {
            if (!ReplaceItem(newItem))
                base.Add(newItem);
        }

        public new void Add(T item)
        {
            AddOrUpdate(item);
        }
    }
}
