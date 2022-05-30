using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace RoflanArchive.Core.Internal
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    internal class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged, INotifyPropertyChanged
        where TKey : notnull
    {
        private const string IndexerName = "Item[]";
        private const string CountName = "Count";
        private const string KeysName = "Keys";
        private const string ValuesName = "Values";



        public event NotifyCollectionChangedEventHandler? CollectionChanged;
        public event PropertyChangedEventHandler? PropertyChanged;



        protected IDictionary<TKey, TValue> Dictionary { get; private set; }



        public ICollection<TKey> Keys
        {
            get
            {
                return Dictionary.Keys;
            }
        }
        public ICollection<TValue> Values
        {
            get
            {
                return Dictionary.Values;
            }
        }
        public int Count
        {
            get
            {
                return Dictionary.Count;
            }
        }
        public bool IsReadOnly
        {
            get
            {
                return Dictionary.IsReadOnly;
            }
        }



        public TValue this[TKey key]
        {
            get
            {
                return Dictionary[key];
            }
            set
            {
                Insert(key, value, false);
            }
        }



        public ObservableDictionary()
        {
            Dictionary = new Dictionary<TKey, TValue>();
        }
        public ObservableDictionary(
            IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(comparer);
        }
        public ObservableDictionary(
            int capacity)
        {
            Dictionary = new Dictionary<TKey, TValue>(capacity);
        }
        public ObservableDictionary(
            int capacity,
            IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }
        public ObservableDictionary(
            IDictionary<TKey, TValue> dictionary)
        {
            Dictionary = new Dictionary<TKey, TValue>(dictionary);
        }
        public ObservableDictionary(
            IDictionary<TKey, TValue> dictionary,
            IEqualityComparer<TKey> comparer)
        {
            Dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
        }



        private void Insert(
            TKey key,
            TValue value,
            bool add)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (!Dictionary.TryGetValue(key, out var item))
            {
                Dictionary[key] = value;

                OnCollectionChanged(
                    NotifyCollectionChangedAction.Add,
                    new KeyValuePair<TKey, TValue>(key, value));

                return;
            }

            if (add)
                throw new ArgumentException("An item with the same key has already been added.");
            if (Equals(item, value))
                return;

            Dictionary[key] = value;

            OnCollectionChanged(
                NotifyCollectionChangedAction.Replace,
                new KeyValuePair<TKey, TValue>(key, value),
                new KeyValuePair<TKey, TValue>(key, item));
        }



        public void Add(
            TKey key,
            TValue value)
        {
            Insert(key, value, true);
        }
        public void Add(
            KeyValuePair<TKey, TValue> item)
        {
            Insert(item.Key, item.Value, true);
        }

        public void AddRange(
            IDictionary<TKey, TValue> items)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));
            if (items.Count <= 0)
                return;

            if (Dictionary.Count > 0)
            {
                if (items.Keys.Any(key => Dictionary.ContainsKey(key)))
                    throw new ArgumentException("An item with the same key has already been added.");

                foreach (var item in items)
                {
                    Dictionary.Add(item);
                }
            }
            else
            {
                Dictionary = new Dictionary<TKey, TValue>(items);
            }

            OnCollectionChanged(
                NotifyCollectionChangedAction.Add,
                items.ToArray());
        }

        public bool Remove(
            TKey key)
        {
            if (key is null)
                throw new ArgumentNullException(nameof(key));

            if (!Dictionary.TryGetValue(key, out var value))
                return false;

            var removed = Dictionary.Remove(key);

            if (removed)
            {
                OnCollectionChanged(
                    NotifyCollectionChangedAction.Remove,
                    new KeyValuePair<TKey, TValue>(key, value));
            }

            return removed;
        }
        public bool Remove(
            KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool Contains(
            KeyValuePair<TKey, TValue> item)
        {
            return Dictionary.Contains(item);
        }

        public bool ContainsKey(
            TKey key)
        {
            return Dictionary.ContainsKey(key);
        }

        public bool TryGetValue(
            TKey key,
            [MaybeNullWhen(false)] out TValue value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        public void Clear()
        {
            if (Dictionary.Count <= 0)
                return;

            Dictionary.Clear();

            OnCollectionChanged();
        }

        public void CopyTo(
            KeyValuePair<TKey, TValue>[] array,
            int arrayIndex)
        {
            Dictionary.CopyTo(array, arrayIndex);
        }



        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)Dictionary).GetEnumerator();
        }
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }



        private void OnPropertyChanged()
        {
            OnPropertyChanged(IndexerName);
            OnPropertyChanged(CountName);
            OnPropertyChanged(KeysName);
            OnPropertyChanged(ValuesName);
        }
        protected virtual void OnPropertyChanged(
            string propertyName)
        {
            PropertyChanged?.Invoke(this,
                new PropertyChangedEventArgs(
                    propertyName));
        }



        private void OnCollectionChanged()
        {
            OnPropertyChanged();

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    NotifyCollectionChangedAction.Reset));
        }
        private void OnCollectionChanged(
            NotifyCollectionChangedAction action,
            KeyValuePair<TKey, TValue> changedItem)
        {
            OnPropertyChanged();

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    action, changedItem));
        }
        private void OnCollectionChanged(
            NotifyCollectionChangedAction action,
            KeyValuePair<TKey, TValue> newItem,
            KeyValuePair<TKey, TValue> oldItem)
        {
            OnPropertyChanged();

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    action, newItem, oldItem,
                    Dictionary.ToList().IndexOf(newItem)));
        }
        private void OnCollectionChanged(
            NotifyCollectionChangedAction action,
            IList newItems)
        {
            OnPropertyChanged();

            CollectionChanged?.Invoke(this,
                new NotifyCollectionChangedEventArgs(
                    action, newItems));
        }
    }
}
