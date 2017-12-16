using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChapterSix_PLINQ
{
    class Program
    {
        /*          System.Ling.ParallelEnumerable 类可以访问大部分PLINQ增加的功能，介绍PLINQ特定的方法
         * 
         *  AsOrdered()     PLINQ必须在剩余的查询中保持原始序列的顺序，直到使用orderby字句对其
         *                  进行改变，或者使用AsUnordered关闭为止
         *                  
         *  AsParallel()    在可能的情况下，剩余的查询应该并行执行
         *  
         *  AsSequential()  与传统的LINQ一样，剩余的查询应该串行执行
         *  
         *  AsUnordered()   对于剩余的查询，PLINQ不需要保持原始序列的顺序
         *  
         *  ForAll()        通过这个枚举方法，可以使用多个任务并行地处理结果
         *  
         *  WithCancellation() 将这个方法和取消标记一起使用，可以允许取消查询的执行
         *  
         *  WithDegreeOfParallelism() PLINQ会根据可用内核总数等于这个方法传入的并行度参数进行优化
         *  
         *  WithExecutionMode() 如果默认行为是按照传统LINQ串行运行，那么这个方法可以强制并行执行
         * 
         *  WithMergeOptions() 通过这个方法可以提供PLINQ合并并行运行结果方式的提示，运行结果
         *                  是由执行查询的线程提供的
         *  
         */

        static void Main(string[] args)
        {
            Test();

            TestSeqReductionQuery();
            TestSeqReductionQuery2();

            //AggregateTest();
            //AggregateTest2();

            ForAllTest();

            MapReduceTest();

            Console.ReadKey();
        }

        private static string[] _words = {
                    "Day", "Car", "Land", "Road", "Mounttain",
                    "River", "Sea", "Shore", "Mouse"};

        static int CountLetters(string key)
        {
            int letters = 0;
            for (int i = 0; i < key.Length; i++)
            {
                if (char.IsLetter(key, i))
                    letters++;
            }

            return letters;
        }

        static void Test()
        {
            var query = from word in _words.AsParallel().AsOrdered() 
                        where word.Contains("a")
                        select word;

            foreach (var result in query)
            {
                Console.WriteLine(result);
            }
        }

        static void Test2()//强制并行查询
        {
            var query = from word in _words.AsParallel().WithExecutionMode(ParallelExecutionMode.ForceParallelism)
                        where word.Contains("a")
                        select word;

            foreach (var result in query)
            {
                Console.WriteLine(result);
            }
        }


        /*      PLINQ执行引擎会使用以下4个主要的算法来进行数据分区
         * 
         * 1 范围分区   这种方式可以用于可索引的数据源    
         * 
         * 2 数据块分区 这种分区方式可以用于任何数据源，当PLINQ查询用于不可索引的数据源时，不同任务请求数据
         *              时得到的数据是按块获得的，而且数据块的大小可能不一样
         * 
         * 3 交错式分区 这种分区方式对在数据源顶部对数据项进行处理的情况下进行额优化，
         *              因此，当查询包含SkipWhile或TakeWhile的时候，PLINQ就会使用这种方式
         *              
         * 4 散列分区   这种方式对数据元素的比较进行了优化，数据和任务之间建立了通道，带有相同散列码
         *              的数据项会被发送到一个任务中。
         */
        static void Test3() //分区再聚合
        {
            var query = (from word in _words.AsParallel()
                         where word.Contains('a')
                         select CountLetters(word)).Sum();
            Console.WriteLine($"The totle number of letters for the words that contain an 'a' is {query}");
        }

        static int NUM_INTS = 50000000;

        static IEnumerable<int> GenerateInputData()
        {
            return Enumerable.Range(1, NUM_INTS);
        }

        static void TestSeqReductionQuery()//LINQ 平均数归约操作
        {
            var inputIntegers = GenerateInputData();

            var sw = Stopwatch.StartNew();
            var seqReductionQuery = (from intNum in inputIntegers
                                     where (intNum % 5 == 0)
                                     select intNum / Math.PI).Average();

            Console.WriteLine($"Average {seqReductionQuery} Time: {sw.Elapsed.ToString()}");
        }

        static ParallelQuery<int> GenerateInputData1()
        {
            return ParallelEnumerable.Range(1, NUM_INTS);
        }

        static void TestSeqReductionQuery2()//PLINQ 平均归约操作
        {
            var inputIntegers = GenerateInputData1();

            var sw = Stopwatch.StartNew();
            var parReductionQuery = (from intNum in inputIntegers.AsParallel()
                                     where intNum % 5 == 0
                                     select intNum / Math.PI).Average();

            Console.WriteLine($"Paralle Average {parReductionQuery} Time: {sw.Elapsed.ToString()}");
        }

        //创建自定义的PLINQ聚合函数  计算标准差、偏度、峰度
        /*              Aggregate方法四个参数介绍
         * 1    累加器函数的初始值
         * 
         * 2    更新累加器函数----对分区中的每一个元素都会调用这个函数
         * 
         * 3    合并累加器函数----这个函数用于根据每个分区的结果计算最终结果
         * 
         * 4    结果选择器----在所有分区的累加值都计算完成之后，结果选择器会将这个结果
         *                    转换为所需的最终结果
         */
        static void AggregateTest()
        {
            Console.WriteLine("自定义聚合函数");
            int[] inputIntegers = { 0, 3, 4, 8, 15, 22, 34, 57, 68, 32, 21, 30 };

            var mean = inputIntegers.AsParallel().Average();

            var standardDeviation = inputIntegers.AsParallel().Aggregate(0d,
                (subTotal, thisNumber) => subTotal + Math.Pow((thisNumber - mean), 2),
                (total, thisNum) => total + thisNum,
                finalSum => Math.Sqrt((finalSum / (inputIntegers.Count() - 1))));

            var skewness = inputIntegers.AsParallel().Aggregate(0d,
                (subTotal, thisNum) => subTotal + Math.Pow(((thisNum - mean) / standardDeviation), 3),
                (total, thisTask) => total + thisTask,
                finalSum => ((finalSum * inputIntegers.Count()) / ((inputIntegers.Count() - 1) * (inputIntegers.Count() - 2))));

            var kurtosis = inputIntegers.AsParallel().Aggregate(0d,
                (subTotal, thisNumber) => subTotal + Math.Pow(((thisNumber - mean) / standardDeviation), 4),
                (total, thisTask) => total + thisTask,
                finalSum => ((finalSum * inputIntegers.Count() * (inputIntegers.Count() + 1)) / ((inputIntegers.Count() - 1) * (inputIntegers.Count() - 2) * (inputIntegers.Count() - 3))) - ((3 * Math.Pow((inputIntegers.Count() - 1), 2)) / ((inputIntegers.Count() - 2) * (inputIntegers.Count() - 3))));

            Console.WriteLine($"Mean: {mean}");
            Console.WriteLine($"Standard deviation: {standardDeviation}");
            Console.WriteLine($"Skewness: {skewness}");
            Console.WriteLine($"Kurtosis: {kurtosis}");
        }


        private static ParallelQuery<int> _inputIntegers = ParallelEnumerable.Range(1, 100000000);
        static double CalculateMean(CancellationToken ct)
        {
            return _inputIntegers.AsParallel().WithCancellation(ct).Average();
        }

        static double CalculateStandardDeviation(CancellationToken ct, double mean)
        {
            return _inputIntegers.AsParallel().WithCancellation(ct).Aggregate(0d,
                (subTotal, thisNumber) => subTotal + Math.Pow((thisNumber - mean), 2),
                (total, thisTask) => total + thisTask,
                finalSum => Math.Sqrt((finalSum / (_inputIntegers.Count() - 1))));
        }

        static double CalculateSkewness(CancellationToken ct, double mean, double standardDeviation)
        {
            return _inputIntegers.AsParallel().WithCancellation(ct).Aggregate(0d,
                (subTotal, thisNumber) => subTotal + Math.Pow(((thisNumber - mean) / standardDeviation), 3),
                (total, thisTask) => total + thisTask,
                finaSum => ((finaSum * _inputIntegers.Count()) / ((_inputIntegers.Count() - 1) *
                (_inputIntegers.Count() - 2))));
        }

        static double CalculateKurtosis(CancellationToken ct, double mean, double standardDeviation)
        {
            return _inputIntegers.AsParallel().WithCancellation(ct).Aggregate(0d,
                (subTotal, thisNumber) => subTotal + Math.Pow(((thisNumber - mean) / standardDeviation), 4),
                (total, thisTask) => total + thisTask,
                finalSum =>
                ((finalSum * _inputIntegers.Count() *
                (_inputIntegers.Count() + 1)) /
                ((_inputIntegers.Count() - 1) *
                (_inputIntegers.Count() - 2) *
                (_inputIntegers.Count() - 3))) -
                ((3 * Math.Pow((_inputIntegers.Count() - 1), 2)) /
                ((_inputIntegers.Count() - 2) *
                (_inputIntegers.Count() - 3))));
        }

        static void AggregateTest2()//结合任务和延续使用PLINQ查询
        {
            Console.WriteLine("自定义聚合函数2");
            var cts = new CancellationTokenSource();
            var ct = cts.Token;
            var sw = Stopwatch.StartNew();
            var taskMean = new Task<double>(() => CalculateMean(ct), ct);

            var taskSTDev = taskMean.ContinueWith<double>(t =>
            {
                return CalculateStandardDeviation(ct, t.Result);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            var taskSkewness = taskSTDev.ContinueWith<double>(t =>
            {
                return CalculateSkewness(ct, taskMean.Result, t.Result);
            }, TaskContinuationOptions.OnlyOnRanToCompletion);

            var taskKurtosis = taskSTDev.ContinueWith<double>(t =>
              {
                  return CalculateKurtosis(ct, taskMean.Result, t.Result);
              }, TaskContinuationOptions.OnlyOnRanToCompletion);

            var deferredCancelTask = Task.Factory.StartNew(() =>
            {
                Thread.Sleep(5000);
                cts.Cancel();
            });

            try
            {
                taskMean.Start();
                Task.WaitAll(taskSkewness, taskKurtosis);
                Console.WriteLine($"Mean: {taskMean.Result}");
                Console.WriteLine($"Standard deviation: {taskSTDev.Result}");
                Console.WriteLine($"Skewness: {taskSkewness.Result}");
                Console.WriteLine($"Kurtosis: {taskKurtosis.Result}");
            }
            catch (AggregateException ex)
            {
                foreach (Exception innerEx in ex.InnerExceptions)
                {
                    Console.WriteLine(innerEx.ToString());
                    if (ex.InnerException is OperationCanceledException)
                    {
                        Console.WriteLine($"Meann task: {taskMean.Status}");
                        Console.WriteLine($"Standard deviation task: {taskSTDev.Status}");
                        Console.WriteLine($"Skewness task: {taskKurtosis.Status}");
                        Console.WriteLine($"Kurtosis task: {taskKurtosis.Status}");
                    }
                }
            }
        }

        //ForAll 方法
        const int NUM_MD5_HASHEX = 100000;

        static ParallelQuery<int> GenerateMD5InputData()
        {
            return ParallelEnumerable.Range(1, NUM_MD5_HASHEX);
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

        static string GenerateMD5Hash(int number)
        {
            var md5M = MD5.Create();
            byte[] data = Encoding.Unicode.GetBytes(Environment.UserName + number.ToString());
            byte[] result = md5M.ComputeHash(data);
            string hexSting = ConvertToHexString(result);

            return hexSting;
        }

        static void ForAllTest()
        {
            Console.WriteLine("ForAll Begin");
            var sw = Stopwatch.StartNew();
            var inputIntegers = GenerateMD5InputData();
            var hashBag = new ConcurrentBag<string>();
            inputIntegers.ForAll(i => hashBag.Add(GenerateMD5Hash(i)));

            Console.WriteLine($"Finished in {sw.Elapsed.ToString()}");
            Console.WriteLine($"{hashBag.Count} MD5 hashes generated in {sw.Elapsed.ToString()}");
            Console.WriteLine($"First MD5 hash: {hashBag.First()}");
            Console.WriteLine($"Last MD5 hash: {hashBag.Last()}");
        }

        //使用两个PLINQ查询实现的一个简单的MapReduce算法
        static List<string> words = new List<string> {
            "there", "is", "a", "great", "house", "and", "an", "amazing","lake",
            "here", "is","a", "computer", "running","a","new","query","there",
            "is","a","great","server","ready","to","process","map","and","reduce"
        };

        static void MapReduceTest()
        {
            Console.WriteLine("MapReduce Begin");

            ILookup<string, int> map = words.AsParallel().ToLookup(p => p, k => 1);
            //End of Map

            var reduce = from IGrouping<string, int> wordMap in map.AsParallel()
                         where wordMap.Count() > 1
                         select new { Word = wordMap.Key, Count = wordMap.Count() };
            //End of Reduce

            foreach (var word in reduce)
            {
                Console.WriteLine($"Word: {word.Word}; Count: {word.Count}");
            }
        }
    }
}
