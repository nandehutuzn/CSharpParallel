using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChapterFour_Concurrent
{
    class PipelineFilter<TInput, TOutput>
    {
        private Func<TInput, TOutput> _processor = null;
        private Action<TInput> _outputProcessor = null;
        private System.Threading.CancellationToken _token;
        public BlockingCollection<TInput>[] Input;
        public BlockingCollection<TOutput>[] Output = null;
        public string Name { get; set; }
        private const int OUT_COLLECTIONS = 5;
        private const int OUT_BOUNDING_CAPACITY = 1000;
        private const int TIMEOUT = 50;
        private const int NUM_SENTENCES = 2000000;

        public PipelineFilter(BlockingCollection<TInput>[] input, Func<TInput, TOutput> processor, System.Threading.CancellationToken token, string name)
        {
            Input = input;
            Output = new BlockingCollection<TOutput>[OUT_COLLECTIONS];
            for (int i = 0; i < Output.Length; i++)
            {
                Output[i] = new BlockingCollection<TOutput>(OUT_BOUNDING_CAPACITY);
            }
            _processor = processor;
            _token = token;
            Name = name;
        }

        public PipelineFilter(BlockingCollection<TInput>[] input, Action<TInput> renderer, System.Threading.CancellationToken token, string name)
        {
            Input = input;
            _outputProcessor = renderer;
            _token = token;
            Name = name;
        }

        public void Run()
        {
            while (!Input.All(inputBC => inputBC.IsCompleted) && !_token.IsCancellationRequested)
            {
                TInput item;
                int i = BlockingCollection<TInput>.TryTakeFromAny(Input, out item, TIMEOUT, _token);
                if (i >= 0)
                {
                    if (Output != null)
                    {
                        TOutput result = _processor(item);
                        BlockingCollection<TOutput>.AddToAny(Output, result, _token);
                    }
                    else
                        _outputProcessor(item);
                }
            }
            if (Output != null)
            {
                foreach (var outputBC in Output)
                {
                    outputBC.CompleteAdding();
                }
            }
        }

        private static string ProduceASentence(string[] possibleSentences)
        {
            var rnd = new Random();
            var sb = new StringBuilder();
            string newSentence;
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
                newSentence = sb.ToString();
            }
            else
            {
                newSentence = sb.ToString().ToUpper();
            }

            return newSentence;
        }
    }
}
