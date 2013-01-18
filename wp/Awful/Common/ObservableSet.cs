using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Awful.Common
{
    /// <summary>
    /// Herp Derp, CollectionDataContracts (such as ObservableSet) cannot have
    /// Serializable custom properties...so this wrapper class was born. All so I can 
    /// keep track of update times...
    /// </summary>
    /// <typeparam name="T">Any object or struct.</typeparam>
    [DataContract]
    public abstract class ObservableSetWrapper<T> : IList<T>
    {
        public ObservableSetWrapper(IEnumerable<T> collection)
        {
            if (collection != null)
                _items = new ObservableSet<T>(collection);
            else 
                _items = new ObservableSet<T>();
        }

        public ObservableSetWrapper() : this(null) { }

        private ObservableSet<T> _items;
        [DataMember]
        public ObservableSet<T> Items
        {
            get 
            {
                if (_items == null)
                    _items = new ObservableSet<T>();

                return _items;
            }

            set { _items = value; }
        }

        public int IndexOf(T item)
        {
            return Items.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            Items.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Items.RemoveAt(index);
        }

        public T this[int index]
        {
            get
            {
                return Items[index];
            }
            set
            {
                Items[index] = value;
            }
        }

        public void Add(T item)
        {
            Items.Add(item);
        }

        public void Clear()
        {
            Items.Clear();
        }

        public bool Contains(T item)
        {
            return Items.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get 
            {
                return Items.Count; 
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(T item)
        {
            return Items.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Items.GetEnumerator();
        }
    }

    [CollectionDataContract]
    public sealed class ObservableSet<T> : ObservableCollection<T>, IEqualityComparer<T>
    {
        public delegate int HashCodeDelegate(object obj);
        public HashCodeDelegate HashCodeMethod { get; set; } 

        public ObservableSet()
            : base()
        {
           
        }

        public ObservableSet(IEnumerable<T> items)
            : base(items)
        {
          
        }

        public bool Equals(T x, T y)
        {
            return GetHashCode(x).Equals(GetHashCode(y));
        }

        public int GetHashCode(T obj)
        {
            if (HashCodeMethod != null)
                return HashCodeMethod(obj);
            else
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

        private void AddOrUpdate(T newItem)
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
