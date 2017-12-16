using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter5
{
    class CountdownEventTest
    {
        /*      CountdownEvent 是一个非常轻量级的同步原语，与通过Task.WaitAll或TaskFactory.ContinueWhenAll等待其他任务
         *      完成执行而运行代码相比，CountdownEvent的开销要小得多。
         *      每当一个任务完成工作的时候，这个任务都会发出一个CountdownEvent实例的信号
         *      ，并将其信号计数递减1，调用这个CountdownEvent实例的Wait方法的任务将会阻塞，
         *      直到信号计数达到0.
         */

        private static CountdownEvent _countdown;
        private static int MIN_PATHS = Environment.ProcessorCount;
        private static int MAX_PATHS = Environment.ProcessorCount * 3;

        private static void SimulatePaths(int pathCount)
        {
            for (int i = 0; i < pathCount; i++)
            {
                Task.Factory.StartNew(num =>
                {
                    try
                    {
                        var pathNumber = (int)num;
                        var sw = Stopwatch.StartNew();
                        var rnd = new Random();
                        Thread.Sleep(rnd.Next(2000, 5000));
                        Console.WriteLine($"Path {pathCount} simulated");
                    }
                    finally
                    {
                        _countdown.Signal();
                    }
                }, i);
            }
        }

        public static void Test()
        {
            _countdown = new CountdownEvent(MIN_PATHS);

            var t1 = Task.Factory.StartNew(() =>
            {
                for (int i = MIN_PATHS; i <= MAX_PATHS; i++)
                {
                    Console.WriteLine($">>>> {i} Concurrent paths start");
                    _countdown.Reset(i);
                    SimulatePaths(i);
                    _countdown.Wait();
                    Console.WriteLine($"<<<<{i} Concurrent paths end");
                }
            });
            try
            {
                t1.Wait();
                Console.WriteLine("The simulation was executed");
            }
            finally
            {
                _countdown.Dispose();
            }
        }
    }
}
