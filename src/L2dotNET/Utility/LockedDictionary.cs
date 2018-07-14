using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace L2dotNET.Utility
{
    public sealed class LockedDictionary<TKey, TValue>
    {
        public int Count => _dictionary.Count;

        private readonly ReaderWriterLockSlim _lock;
        private readonly Dictionary<TKey, TValue> _dictionary;

        public LockedDictionary(int capacity)
        {
            _lock = new ReaderWriterLockSlim();
            _dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        public IEnumerable<TValue> GetAll()
        {
            try
            {
                _lock.EnterReadLock();
                return _dictionary.Select(x => x.Value).ToList();
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public void Add(TKey key, TValue value)
        {
            try
            {
                _lock.EnterWriteLock();
                if (!_dictionary.ContainsKey(key))
                {
                    _dictionary.Add(key, value);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public void Remove(TKey key)
        {
            try
            {
                _lock.EnterWriteLock();
                if (_dictionary.ContainsKey(key))
                {
                    _dictionary.Remove(key);
                }
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
    }
}
