using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("KeyedLocks.Tests")]

namespace KeyedLocks
{
    public sealed class KeyedLock<T> where T : notnull
    {
        internal readonly Dictionary<T, Entry> Locks;
        private readonly int MaxCount;

        public KeyedLock() : this(null, 1) { }
        public KeyedLock(IEqualityComparer<T>? comparer) : this(comparer, 1) { }
        public KeyedLock(IEqualityComparer<T>? comparer, int maxCount)
        {
            Locks = new Dictionary<T, Entry>(comparer);
            MaxCount = maxCount;
        }

        private Entry PreLock(T key)
        {
            lock (Locks)
            {
                if (!Locks.TryGetValue(key, out var entry))
                {
                    entry = new Entry(this, key, MaxCount);
                    Locks.Add(key, entry);
                }
                Interlocked.Increment(ref entry.Refs);
                return entry;
            }
        }
        public Entry Lock(T key)
        {
            var entry = PreLock(key);
            entry.Semaphore.Wait();
            return entry;
        }
        public async Task<Entry> LockAsync(T key)
        {
            var entry = PreLock(key);
            await entry.Semaphore.WaitAsync();
            return entry;
        }

        private void Unlock(Entry entry)
        {
            if (Interlocked.Decrement(ref entry.Refs) <= 0)
            {
                lock (Locks)
                {
                    if (entry.Refs <= 0)
                    {
                        Locks.Remove(entry.Key);
                        return;
                    }
                }
            }
            entry.Semaphore.Release();
        }

        public sealed class Entry : IDisposable
        {
            public readonly KeyedLock<T> Lock;
            public readonly T Key;
            internal readonly SemaphoreSlim Semaphore;
            internal volatile int Refs;

            internal Entry(KeyedLock<T> keyedLock, T key, int maxCount)
            {
                Lock = keyedLock;
                Key = key;
                Semaphore = new SemaphoreSlim(maxCount, maxCount);
                Refs = 0;
            }

            public void Dispose()
            {
                Lock.Unlock(this);
            }
        }
    }
}
