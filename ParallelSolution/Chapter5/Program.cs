using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter5
{
    class Program
    {
        static void Main(string[] args)
        {
            BarrierTest.Test();
            BarrierTest.TestThrowException();
            BarrierTest.TestTimeout();

            BarrierTest.BarrierAndSpinLockTest();
            ManualResetEventSlimTest.ManualResetEventSlimExample();

            SemaphoreSlimTest.Test();
            CountdownEventTest.Test();

            Console.ReadKey();
        }
    }
}
