using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace KeyedLocks.Tests
{
    [TestClass]
    public class TestCoyote
    {
        [TestMethod]
        public void TestDeadlock()
        {
            var a = new object();
            var b = new object();

            var ab = Task.Run(() =>
            {
                lock (a)
                {
                    lock (b)
                    {
                        Debug.WriteLine("ab");
                    }
                }
            });
            var ba = Task.Run(() =>
            {
                lock (b)
                {
                    lock (a)
                    {
                        Debug.WriteLine("ba");
                    }
                }
            });

            Task.WaitAll(ab, ba);
        }

        [TestMethod]
        public void TestRace()
        {
            int i = 0;

            Action increment = () =>
            {
                var I = i;
                Microsoft.Coyote.Runtime.SchedulingPoint.Interleave(); //Coyote seems to need help to expose data races
                i = I + 1;
            };

            Task.WaitAll(Task.Run(increment), Task.Run(increment));

            Assert.AreEqual(2, i);
        }
    }
}
