using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter5
{
    class ManualResetEventSlimTest
    {
        /*      ManualResetEventSlim  简介
         * 构造函数  第一个bool参数表示初始状态，第二个int参数表示在回退到基于内核的
         *           的等待之前自旋等待的次数
         * 实例方法
         * Reset 将事件设置为false（取消设置/取消信号）
         * Set   将事件设置为true（设置/发出信号），如果有任务调用了Wait方法，那么这些任务
         *       将会在这个方法被调用的时候解除阻塞
         * Wait  阻塞当前任务或线程，直到另一个任务或线程通过调用这个实例的Set方法将这个实例设置为true。
         *       如果这个方法已经是设置/发出信号，那么这个方法会立即返回，Wait总数会执行
         *       自旋等待的过程，然后再执行基于内核的等待，即使在构造函数中没有指定spincount值
         *       也是如此
         * 只读属性
         * IsSet 表明事件是否为true
         * SpinCount 进入基于内核等待之前要执行的自旋次数
         * WaitHandle 提供了封装操作系统对象的WaitHandle对象，通过这个对象可以等待对共享资源的排他访问
         */


        private const int NUM_SENTENCES = 2000000;
        private static ConcurrentBag<string> _sentencesBag;
        private static ConcurrentBag<string> _capWordsInSentencesBag;
        private static ConcurrentBag<string> _finalSentencesBag;

        private static ManualResetEventSlim _mresProduceSentences;
        private static ManualResetEventSlim _mresCapitalizeWords;

        private const int TIMEOUT = 5000;

        private static string CapitalizeWords(char[] delimiters, string sentence, char newDelimiter)
        {
            string[] words = sentence.Split(delimiters);
            var sb = new StringBuilder();
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 1)
                {
                    sb.Append(words[i][0].ToString().ToUpper());
                    sb.Append(words[i].Substring(1).ToLower());
                }
                else
                    sb.Append(words[i].ToLower());
            }
            return sb.ToString();
        }

        private static void ProduceSentences()
        {
            string[] possibleSentences = {
                "ConcurrentBag is included  in the Systems.Concurrent.Collections namespace",
                "Is parallelism important for cloud-computing?",
                "Parallelism is very important for cloud-computing!",
                "ConcurrentQueue is one of the new concurrent collections added in .NET Framework4",
                "ConcurrentStack is a concurrent collection that repressents a LIFO collection",
                "ConcurrentQueue is a concurrent collection that repreents a FIFO collection"
            };

            try
            {
                _mresProduceSentences.Set();

                var rnd = new Random();
                for (int i = 0; i < NUM_SENTENCES; i++)
                {
                    var sb = new StringBuilder();
                    for (int j = 0; j < possibleSentences.Length; j++)
                    {
                        if (rnd.Next(2) > 0)
                        {
                            sb.Append(possibleSentences[rnd.Next(possibleSentences.Length)]);
                            sb.Append(' ');
                        }
                    }
                    if (rnd.Next(20) > 15)
                        _sentencesBag.Add(sb.ToString());
                    else
                        _sentencesBag.Add(sb.ToString().ToUpper());
                }
            }
            finally
            {
                _mresProduceSentences.Reset();
            }
        }

        private static void CapitalizeWordsInSentences()
        {
            char[] delimiterChars = { ' ', ',', '.', ':', ';', '(', ')', '[', ']', '{', '}', '/', '?', '@', '\t', '"' };

            _mresProduceSentences.Wait();
            try
            {
                _mresCapitalizeWords.Set();

                while (!_sentencesBag.IsEmpty || _mresProduceSentences.IsSet)
                {
                    string sentence;
                    if (_sentencesBag.TryTake(out sentence))
                    {
                        _capWordsInSentencesBag.Add(CapitalizeWords(delimiterChars, sentence, '\\'));
                    }
                }
            }
            finally
            {
                _mresCapitalizeWords.Reset();
            }
        }

        private static string RemoveLetters(char[] letters, string sentence)
        {
            var sb = new StringBuilder();
            bool match = false;

            for (int i = 0; i < sentence.Length; i++)
            {
                for (int j = 0; j < letters.Length; j++)
                {
                    if (sentence[i] == letters[j])
                    {
                        match = true;
                        break;
                    }
                }
                if (!match)
                    sb.Append(sentence[i]);
                match = false;
            }

            return sb.ToString();
        }

        private static void RemoveLettersInSentences()
        {
            char[] letterChars = { 'A', 'B', 'C', 'e', 'i', 'j', 'm', 'X', 'Y', 'Z' };

            _mresCapitalizeWords.Wait();

            while (!_capWordsInSentencesBag.IsEmpty || _mresCapitalizeWords.IsSet)
            {
                string sentence;
                if (_capWordsInSentencesBag.TryTake(out sentence))
                {
                    _finalSentencesBag.Add(RemoveLetters(letterChars, sentence));
                }
            }
        }

        public static void ManualResetEventSlimExample()
        {
            Console.WriteLine("ManualResetEventSlimExample Begin");
            _sentencesBag = new ConcurrentBag<string>();
            _capWordsInSentencesBag = new ConcurrentBag<string>();
            _finalSentencesBag = new ConcurrentBag<string>();

            _mresProduceSentences = new ManualResetEventSlim(false, 100);
            _mresCapitalizeWords = new ManualResetEventSlim(false, 100);
            var sw = Stopwatch.StartNew();

            try
            {
                Parallel.Invoke(
                    () => ProduceSentences(),
                    () => CapitalizeWordsInSentences(),
                    () => RemoveLettersInSentences());
            }
            finally
            {
                _mresCapitalizeWords.Dispose();
                _mresProduceSentences.Dispose();
            }

            Console.WriteLine($"Finished Time: {sw.Elapsed.ToString()}");
            Console.WriteLine($"Number of sentences with capitalized words in the bag: {_capWordsInSentencesBag.Count}");
            Console.WriteLine($"Number of sentences with removed letters in the bag: {_finalSentencesBag.Count}");
            
        }
    }
}
