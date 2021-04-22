using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KeyedLocks.Tests
{
    [TestClass]
    public class KeyedLock
    {
        volatile int parallel = 0;
        volatile int maxParallel = 0;

        private async Task LockerTask<T>(KeyedLock<T> locker, T key) where T : notnull
        {
            using (await locker.LockAsync(key))
            {
                //Increment counter...
                var newMax = Interlocked.Increment(ref parallel);
                //Ensure that the maximum is updated, if necessary...
                Debug.WriteLine("Maximum: {0}", newMax);
                var oldMax = maxParallel;
                while (newMax > oldMax && Interlocked.CompareExchange(ref maxParallel, newMax, oldMax) != oldMax)
                {
                    oldMax = maxParallel;
                }
                //Wait a little bit, so we get things overlapping in time...
                await Task.Delay(10);
                //Decrement counter...
                Interlocked.Decrement(ref parallel);
            }
        }

        [TestMethod]
        public void Parallel()
        {
            parallel = 0;
            maxParallel = 0;

            var locker = new KeyedLock<string>();

            Task.WaitAll(
                Task.Run(async () => await LockerTask(locker, "a")),
                Task.Run(async () => await LockerTask(locker, "b"))
            );

            Assert.IsTrue(1 <= maxParallel && maxParallel <= 2);
            lock (locker.Locks)
            {
                Assert.AreEqual(0, locker.Locks.Count);
            }
        }

        [TestMethod]
        public void Parallel2()
        {
            parallel = 0;
            maxParallel = 0;

            var locker = new KeyedLock<string>();

            Task.WaitAll(
                Task.Run(async () => await LockerTask(locker, "a")),
                Task.Run(async () => await LockerTask(locker, "a")),
                Task.Run(async () => await LockerTask(locker, "b"))
            );

            Assert.IsTrue(1 <= maxParallel && maxParallel <= 2);
            lock (locker.Locks)
            {
                Assert.AreEqual(0, locker.Locks.Count);
            }
        }

        [TestMethod]
        public void Series()
        {
            parallel = 0;
            maxParallel = 0;

            var locker = new KeyedLock<string>();

            Task.WaitAll(
                Task.Run(async () => await LockerTask(locker, "a")),
                Task.Run(async () => await LockerTask(locker, "a"))
            );

            Assert.AreEqual(1, maxParallel);
            lock (locker.Locks)
            {
                Assert.AreEqual(0, locker.Locks.Count);
            }
        }
    }
}
