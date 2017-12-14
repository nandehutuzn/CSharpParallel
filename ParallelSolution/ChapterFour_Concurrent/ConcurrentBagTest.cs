using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChapterFour_Concurrent
{
    class ConcurrentBagTest
    {
        /*  ConcurrentBag 在同一个线程添加元素和删除元素的场合下效率特别高，ConcurrentBag使用了很多不同的机制，最大程度地减少了同步的需求以及同步所带来的开销，
         * 然而ConcurrentBag有时候会需要锁，因此，在生产者线程和消费者线程完全分开的场景下，ConcurrentBag的效率非常低下，ConcurrentBag为每一个访问集合的线程
         * 维护了一个本地队列，而且在可能的情况下，ConcurrentBag会以无锁的方式访问这个本地队列，而这种方式可能带有很少的资源争用或者完全没有任何资源争用，ConcurrentBag
         * 表示一个无序组，即一个无序的对象集合，而且支持对象重复，因此，当不用考虑顺序的时候，ConcurrentBag对于存储和访问对象非常有用，常用如下3个方法：
         * 1        Add----将作为参数接收到的新元素添加到无序组中。
         * 2        TryTake--尝试重无序组中删除一个元素，并且将这个元素通过out参数返回。
         * 3        TryPeek----尝试通过out参数从无序组中返回一个元素，但是不删除这个元素
         */


        /*          下例的ConcurrentBag也可换成BlockingCollection  ，但有以下区别
         *    BoundedCapacity属性保存了为集合指定的额最大容量，当集合容量达到这个值得时候，如果有一个添加元素的请求，
         *    那么生产者任务或者线程将会被阻塞，这也就是说，生产者任务或线程必须等待，直到有元素被删除为止。限界功能对于控制内存中集合
         *    的最大大小特别是在需要处理大量元素的时候，非常有用，不需要再生产者和消费者之间使用共享的bool标志或者
         *    添加同步代码，因为BlockingCollection已经提供了是和这种情形的功能。每一个消费者都会尝试从集合中移除元素，直到
         *    生产者的集合的IsCompleted属性设置为true。
        */
        private static ConcurrentBag<string> _sentencesBag;
        private static ConcurrentBag<string> _capWordsInSentencesBag;
        private static ConcurrentBag<string> _finalSentencesBag;
        private static volatile bool _producingSentences = false;
        private static volatile bool _capitalizingWords = false;
        private const int NUM_SENTENCES = 2000000;

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
            string[] possibleSentences = { "ConcurrentBag is included  in the Systems.Concurrent.Collections namespace",
                "Is parallelism important for cloud-computing?",
                "Parallelism is very important for cloud-computing!",
                "ConcurrentQueue is one of the new concurrent collections added in .NET Framework4",
                "ConcurrentStack is a concurrent collection that repressents a LIFO collection",
                "ConcurrentQueue is a concurrent collection that repreents a FIFO collection"};

            try
            {
                _sentencesBag = new ConcurrentBag<string>();
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
                    {
                        _sentencesBag.Add(sb.ToString());
                    }
                    else
                    {
                        _sentencesBag.Add(sb.ToString().ToUpper());
                    }
                }
            }
            finally
            {
                _producingSentences = false;
            }
        }

        private static void CapitalizeWordsInSentences()
        {
            char[] delimiterChars = { ' ', ',', '.', ':', ';', '(', ')', '[', ']', '{', '}', '/', '?', '@', '\t', '"' };
            System.Threading.SpinWait.SpinUntil(() => _producingSentences);  //自旋阻塞，直到_producingSentences为true

            try
            {
                _capitalizingWords = true;
                while (!_sentencesBag.IsEmpty || _producingSentences)
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
                _capitalizingWords = false;
            }
        }

        private static void RemoveLettersInSentences()
        {
            char[] letterChars = { 'A', 'B', 'C', 'e', 'i', 'j', 'm', 'X', 'Y', 'Z' };

            System.Threading.SpinWait.SpinUntil(() => _capitalizingWords);

            while (!_capWordsInSentencesBag.IsEmpty || _capitalizingWords)
            {
                string sentence;
                if (_capWordsInSentencesBag.TryTake(out sentence))
                {
                    _finalSentencesBag.Add(RemoveLetters(letterChars, sentence));
                }
            }
        }

        public static void Test()
        {
            Console.WriteLine("ConcurrentBagTest Begin");
            var sw = Stopwatch.StartNew();

            _sentencesBag = new ConcurrentBag<string>();
            _capWordsInSentencesBag = new ConcurrentBag<string>();
            _finalSentencesBag = new ConcurrentBag<string>();

            _producingSentences = true;

            Parallel.Invoke(
                () => ProduceSentences(),
                () => CapitalizeWordsInSentences(),
                () => RemoveLettersInSentences());
            
            Console.WriteLine($"Number of sentences with capitalized words in the bag: {_capWordsInSentencesBag.Count}");
            Console.WriteLine($"Number of sentences with rmeove letters in the bag: {_finalSentencesBag.Count}");
            Console.WriteLine($"Finished: {sw.Elapsed.ToString()}");
        }
    }
}
