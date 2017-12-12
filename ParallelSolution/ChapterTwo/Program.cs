using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace ChapterTwo
{
    class Program
    {
        private const int NUM_AES_KEYS = 800000;
        private const int NUM_MD5_HASHS = 100000;

        static void Main(string[] args)
        {
            ParallelInvoke();
            ParallelInvokeCheck();
            ParallelInvokeCheck2();
            ParallelInvokeCheck3();
            ParallelForEachGenerateMD5HashesBreak();
            Console.ReadKey();
        }

        #region Parallel.Invoke

        /* Parallel.Invoke 需要和注意的地方
         * 1    r如果加载的方法运行时间迥异，那么就需要最长的时间才能返回控制
         * 2    在并行可扩展性方面具有一定的局限性，因为Parallel.Invoke调用的是固定数目的委托，
         *      在下面示例中，如果在具有16个逻辑内核的计算机上运行这个示例，则只能并行地运行4个
         *      方法，因而还有12个内核一直处于闲置状态
         * 3    每一次对这个方法进行调用时，在运行潜在的并行方法之前都会产生一些额外开销
         * 4    与任何并行化的代码一样，不同方法间的任何相关性或不可控的交互都会导致
         *      难以检测的并发bug以及意想不到的副作用
         * 5    方法执行的顺序无法保证
         * 6    所有根据不同的并行执行计划所加载的委托都可能抛出异常，因此，捕捉并处理这些
         *      异常的代码比传统串行代码中进行的异常处理的代码要更复杂
         */
        static void ParallelInvoke()
        {
            Parallel.Invoke(
                () => ConvertEllipses(),
                () => ConvertRectangles(),
                () => ConvertLines(),
                () => ConvertText());
        }

        static void ConvertEllipses()
        {
            Console.WriteLine($"Ellipses converter. Id {Thread.CurrentThread.ManagedThreadId}");
        }

        static void ConvertRectangles()
        {
            Console.WriteLine($"Rectangles converted. Id {Thread.CurrentThread.ManagedThreadId}");
        }

        static void ConvertLines()
        {
            Console.WriteLine($"Lines converted. Id {Thread.CurrentThread.ManagedThreadId}");
        }

        static void ConvertText()
        {
            Console.WriteLine($"Text converted. Id {Thread.CurrentThread.ManagedThreadId}");
        }

        #endregion

        #region Parallel.Invoke 测验

        static void ParallelInvokeCheck()
        {
            Console.WriteLine("单纯方法并行");
            var sw = Stopwatch.StartNew();
            GenerateAESKeys();
            GenerateMD5Hashes();
            Console.WriteLine($"串行时间 { sw.Elapsed.ToString()}");

            sw.Restart();
            Parallel.Invoke(
                () => GenerateAESKeys(),
                () => GenerateMD5Hashes());
            Console.WriteLine($"并行时间 {sw.Elapsed.ToString()}");
        }

        static void ParallelInvokeCheck2()
        {
            Console.WriteLine("子方法并行测试");
            var sw = Stopwatch.StartNew();
            ParallelGenerateAESKeys();
            ParallelGenerateMD5Hashes();
            Console.WriteLine($"串行2时间 { sw.Elapsed.ToString()}");

            sw.Restart();
            Parallel.Invoke(
                () => ParallelGenerateAESKeys(),
                () => ParallelGenerateMD5Hashes());
            Console.WriteLine($"并行2时间 {sw.Elapsed.ToString()}");
        }

        static void ParallelInvokeCheck3()
        {
            Console.WriteLine("分区器并行测试");
            var sw = Stopwatch.StartNew();
            ParallelPartitionGenerateAESKeys();
            ParallelPartitionGenerateMD5Hashes();
            Console.WriteLine($"串行3时间 { sw.Elapsed.ToString()}");

            sw.Restart();
            Parallel.Invoke(
                () => ParallelPartitionGenerateAESKeys(),
                () => ParallelPartitionGenerateMD5Hashes());
            Console.WriteLine($"并行3时间 {sw.Elapsed.ToString()}");
        }

        static string ConvertToHexString(byte[] byteArray)
        {
            var sb = new StringBuilder(byteArray.Length);

            for (int i = 0; i < byteArray.Length; i++)
            {
                sb.Append(byteArray[i].ToString("X2"));
            }

            return sb.ToString();
        }

        private static void GenerateAESKeys()
        {
            var sw = Stopwatch.StartNew();
            var aesM = new AesManaged();
            for (int i = 1; i <= NUM_AES_KEYS; i++)
            {
                aesM.GenerateKey();
                byte[] result = aesM.Key;
                string hesSting = ConvertToHexString(result);
            }
            Console.WriteLine($"AES: {sw.Elapsed.ToString()}");
        }

        private static void ParallelGenerateAESKeys()
        {
            var sw = Stopwatch.StartNew();
            Parallel.For(1, NUM_AES_KEYS + 1, i =>
              {
                  var aesM = new AesManaged();
                  byte[] result = aesM.Key;
                  string hexString = ConvertToHexString(result);
              });
            Console.WriteLine($"AES: {sw.Elapsed.ToString()}");
        }

        private static void ParallelPartitionGenerateAESKeys()
        {
            var sw = Stopwatch.StartNew();
            Parallel.ForEach(Partitioner.Create(1, NUM_AES_KEYS + 1), range =>
               {
                   var aesM = new AesManaged();
                   //Console.WriteLine($"AES Range ({range.Item1}, {range.Item2}. TimeOfDay before inner loop starts: {DateTime.Now.TimeOfDay})");
                   for (int i = range.Item1; i < range.Item2; i++)
                   {
                       aesM.GenerateKey();
                       byte[] result = aesM.Key;
                       string hexString = ConvertToHexString(result);
                   }
               });
            Console.WriteLine($"ParallelPartitionGenerateAESKeys AES: {sw.Elapsed.ToString()}");
        }

        private static void GenerateMD5Hashes()
        {
            var sw = Stopwatch.StartNew();
            var md5M = MD5.Create();
            for (int i = 1; i <= NUM_MD5_HASHS; i++)
            {
                byte[] data = Encoding.Unicode.GetBytes(
                    Environment.UserName + i.ToString());
                byte[] result = md5M.ComputeHash(data);
                string hexSting = ConvertToHexString(result);
            }
            Console.WriteLine($"MD5: {sw.Elapsed.ToString()}");
        }

        private static void ParallelGenerateMD5Hashes()
        {
            var sw = Stopwatch.StartNew();
            Parallel.For(1, NUM_MD5_HASHS + 1, i =>
               {
                   var md5M = MD5.Create();
                   byte[] data = Encoding.Unicode.GetBytes(
                    Environment.UserName + i.ToString());
                   byte[] result = md5M.ComputeHash(data);
                   string hexSting = ConvertToHexString(result);
               });
            Console.WriteLine($"MD5: {sw.Elapsed.ToString()}");
        }

        private static void ParallelPartitionGenerateMD5Hashes()
        {
            var sw = Stopwatch.StartNew();
            Parallel.ForEach(Partitioner.Create(1, NUM_MD5_HASHS + 1), range =>
            {
                var md5M = MD5.Create();
                //Console.WriteLine($"MD5 Range ({range.Item1}, {range.Item2}. TimeOfDay before inner loop starts: {DateTime.Now.TimeOfDay})");
                for (int i = range.Item1; i < range.Item2; i++)
                {
                    byte[]data = Encoding.Unicode.GetBytes(
                    Environment.UserName + i.ToString());
                    byte[] result = md5M.ComputeHash(data);
                    string hexString = ConvertToHexString(result);
                }
            });
            Console.WriteLine($"ParallelPartitionGenerateMD5Hashes MD5 HASH: {sw.Elapsed.ToString()}");
        }

        static IEnumerable<int> GenerateMD5InputData()
        {
            return Enumerable.Range(1, NUM_AES_KEYS);
        }

        static void ParallelForEachGenerateMD5Hashes()  //并没有优化效果
        {
            var inputData = GenerateMD5InputData();
            var sw = Stopwatch.StartNew();
            Parallel.ForEach(inputData, number =>
            {
                var md5M = MD5.Create();
                byte[] data = Encoding.Unicode.GetBytes(
                    Environment.UserName + number.ToString());
                byte[] result = md5M.ComputeHash(data);
                string hexString = ConvertToHexString(result);
            });
            Console.WriteLine($"ParallelForEachGenerateMD5Hashes MD5: {sw.Elapsed.ToString()}");
        }

        static void DisplayParallelLoopResult(ParallelLoopResult loopResult)
        {
            string text;
            if (loopResult.IsCompleted)
            {
                text = "The loop ran to completion";
            }
            else
            {
                if (loopResult.LowestBreakIteration.HasValue)
                {
                    text = "The loop ended by calling the break statement";
                }
                else
                {
                    text = "The loop ended prematurely with a stop statement";
                }
            }
            Console.WriteLine(text);
        }

        static void ParallelForEachGenerateMD5HashesBreak()
        {
            Console.WriteLine("Begin ParallelForEachGenerateMD5HashesBreak");
            var inputData = GenerateMD5InputData();
            var sw = Stopwatch.StartNew();
            var loopResult = Parallel.ForEach(inputData, (number, loopState) =>
            {
                //if (loopState.ShouldExitCurrentIteration)
                //    return;

                var md5M = MD5.Create();
                byte[] data = Encoding.Unicode.GetBytes(
                    Environment.UserName + number.ToString());
                byte[] result = md5M.ComputeHash(data);
                string hexString = ConvertToHexString(result);
                if (sw.Elapsed.Seconds > 3)
                {
                    loopState.Break();
                    return;
                }
            });

            DisplayParallelLoopResult(loopResult);
            Console.WriteLine($"ParallelForEachGenerateMD5HashesBreak MD5: {sw.Elapsed.ToString()}");
        }


        #endregion
    }
}
