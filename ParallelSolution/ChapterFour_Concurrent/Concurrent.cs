using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading;

namespace ChapterFour_Concurrent
{
    /*          几种并发集合     这些集合都在某种程度上使用了无锁技术，因此可以获得性能提升
     * 
     * 1    BlockingCollection<T>----与经典的阻塞队列数据结构类似；在这里，BlockingCollection<T> 能够适用于有多个任务添加和删除数据的
     *      生产者-消费者的情形。BlockingCollection<T>是一个对IProducerConsumer<T>实例的包装器，提供了阻塞和限界的能力。
     * 2    ConcurrentBag<T>----提供了一个无序的对象集合，当不用考虑顺序时非常有用。
     * 3    ConcurrentDictionary<TKey,TValue>----与经典的键-值对的字典类似，提供了并发的键值访问。
     * 4    ConcurrentQueue<T>----一个FIFO(先进先出)的集合，支持很多任务并发地进行元素入队和出队操作。
     * 5    ConcurrentStack<T>----一个LIFO(后进先出)的集合，支持很多任务并发地进行压入和弹出元素操作。
     * 
     */
    class Concurrent
    {
        private static ConcurrentQueue<byte[]> _byteArraysQueue;
        private static ConcurrentQueue<string> _keysQueue;
        private static ConcurrentQueue<string> _validKeys;
        private const int NUM_AES_KEYS = 800000;
        private const int NUM_MD5_HASHS = 100000;
        private static string[] _invalidHexValues = { "AF", "BD", "BF", "CF", "DA", "FA", "FE", "FF" };
        private static int MAX_INVALID_HEX_VALUES = 3;
        private static int tasksHexStringsRunning = 0;


        public static void ConcurrentTest() //多生产者，一个消费者
        {
            _keysQueue = new ConcurrentQueue<string>();
            _byteArraysQueue = new ConcurrentQueue<byte[]>();
            var sw = Stopwatch.StartNew();

            var taskAESKeys = Task.Factory.StartNew(() => ParallelPartitionGenerateAESKeys(Environment.ProcessorCount - 1));
            var taskHexStrings = Task.Factory.StartNew(() => ConvertAESKeysToHex(taskAESKeys));

            string lastKey;

            while (taskHexStrings.Status == TaskStatus.Running || taskHexStrings.Status == TaskStatus.WaitingToRun)
            {
                var countResult = _keysQueue.Count(key => key.Contains("F"));
                Console.WriteLine($"So far, the number of keys that contaion an F is: {countResult}");
                if (_keysQueue.TryPeek(out lastKey))
                {
                    Console.WriteLine($"The first key in the queue is: {lastKey}");
                }
                else
                    Console.WriteLine("No keys yet.");

                System.Threading.Thread.Sleep(500);
            }

            Task.WaitAll(taskAESKeys, taskHexStrings);
            Console.WriteLine($"Number of keys in the list: {_keysQueue.Count}");
            Console.WriteLine($"Time: {sw.Elapsed.ToString()}");
            Console.WriteLine("Finished");
        }

        public static void ConcurrentTest2()  //多生产者，多消费者
        {
            Console.WriteLine("ConcurrentTest2 Begin");
            _keysQueue = new ConcurrentQueue<string>();
            _byteArraysQueue = new ConcurrentQueue<byte[]>();
            int taskAESKeysMax = Environment.ProcessorCount / 2;
            int taskHexStringMax = Environment.ProcessorCount - taskAESKeysMax;
            var sw = Stopwatch.StartNew();
            Task taskAESKeys = Task.Factory.StartNew(() => ParallelPartitionGenerateAESKeys(taskAESKeysMax));
            Task[] tasksHexStrings = new Task[taskHexStringMax];

            for (int i = 0; i < taskHexStringMax; i++)
            {
                tasksHexStrings[i] = Task.Factory.StartNew(() => ConvertAESKeysToHex(taskAESKeys));
            }

            Task.WaitAll(tasksHexStrings);
            Console.WriteLine($"Number of keys in the list: {_keysQueue.Count}");
            Console.WriteLine($"Number of count in byteArray: {_byteArraysQueue.Count}");
            Console.WriteLine($"Finished : {sw.Elapsed.ToString()}");
        }

        public static void ConcurrentTest3() //多生产者，多消费者，一个验证者
        {
            Console.WriteLine("ConcurrentTest3 Begin");

            _byteArraysQueue = new ConcurrentQueue<byte[]>();
            _keysQueue = new ConcurrentQueue<string>();
            _validKeys = new ConcurrentQueue<string>();
            int taskAESKeysMax = Environment.ProcessorCount / 2;
            int taskHexStringsMax = Environment.ProcessorCount - taskAESKeysMax - 1;
            var sw = Stopwatch.StartNew();

            var taskAESKeys = Task.Factory.StartNew(() => ParallelPartitionGenerateAESKeys(taskAESKeysMax));
            Task[] tasksHexStrings = new Task[taskHexStringsMax];
            for (int i = 0; i < taskHexStringsMax; i++)
            {
                Interlocked.Increment(ref tasksHexStringsRunning);
                tasksHexStrings[i] = Task.Factory.StartNew(() =>
                {
                    try
                    {
                        ConvertAESKeysToHex(taskAESKeys);
                    }
                    finally
                    {
                        Interlocked.Decrement(ref tasksHexStringsRunning);
                    }
                });
            }

            var taskValidateKeys = Task.Factory.StartNew(() => ValidKeys());
            taskValidateKeys.Wait();

            Console.WriteLine($"Number of keys in the list: {_keysQueue.Count}");
            Console.WriteLine($"Number of valid keys: {_validKeys.Count}");
            Console.WriteLine($"Finished : {sw.Elapsed.ToString()}");
        }

        private static void ValidKeys()
        {
            var sw = Stopwatch.StartNew();
            while ((tasksHexStringsRunning > 0 || _keysQueue.Count > 0))
            {
                string hexString;
                if (_keysQueue.TryDequeue(out hexString))
                {
                    if (IsKeyValid(hexString))
                        _validKeys.Enqueue(hexString);
                }
            }
            Console.WriteLine($"Validate: {sw.Elapsed.ToString()}");
        }

        private static bool IsKeyValid(string key)
        {
            int count = 0;
            for (int i = 0; i < _invalidHexValues.Length; i++)
            {
                if (key.Contains(_invalidHexValues[i]))
                {
                    count++;
                    if (count == MAX_INVALID_HEX_VALUES)
                        return true;
                    if (((_invalidHexValues.Length - i) + count) < MAX_INVALID_HEX_VALUES)
                        return false;
                }
            }
            return false;
        }

        private static string ConvertToHexString(byte[] byteArray)
        {
            var sb = new StringBuilder(byteArray.Length);

            for (int i = 0; i < byteArray.Length; i++)
            {
                sb.Append(byteArray[i].ToString("X2"));
            }

            return sb.ToString();
        }

        private static void ParallelPartitionGenerateAESKeys(int maxDegree)
        {
            var parallelOptions = new ParallelOptions() { MaxDegreeOfParallelism = maxDegree };
            var sw = Stopwatch.StartNew();
            Parallel.ForEach(Partitioner.Create(1, NUM_AES_KEYS + 1), parallelOptions, range =>
              {
                  var aesM = new AesManaged();
                  //Console.WriteLine($"AES Range ({range.Item1}, {range.Item2})  Time: {DateTime.Now.TimeOfDay}");

                  for (int i = range.Item1; i < range.Item2; i++)
                  {
                      aesM.GenerateKey();
                      byte[] result = aesM.Key;
                      _byteArraysQueue.Enqueue(result);
                  }
              });
            Console.WriteLine($"AES: {sw.Elapsed.ToString()}");
        }

        private static void ConvertAESKeysToHex(Task taskProducer)
        {
            var sw = Stopwatch.StartNew();
            while ((taskProducer.Status == TaskStatus.Running) || (taskProducer.Status == TaskStatus.WaitingToRun) || (_byteArraysQueue.Count > 0))
            {
                byte[] result;
                if (_byteArraysQueue.TryDequeue(out result))
                {
                    string hexString = ConvertToHexString(result);
                    _keysQueue.Enqueue(hexString);
                }
            }

            Console.WriteLine($"HEX: {sw.Elapsed.ToString()}");
        }

    }
}
