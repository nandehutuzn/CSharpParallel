using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter5
{
    class SemaphoreSlimTest
    {
        private static SemaphoreSlim _semaphore;
        private const int MAX_MACHINES = 3;
        private static int _attackers = Environment.ProcessorCount;
        private static Task[] _tasks;

        private static void SimulateAttacks(int attackerNumber)
        {
            var sw = Stopwatch.StartNew();
            var rnd = new Random();
            Thread.Sleep(rnd.Next(2000, 5000));
            Console.WriteLine($"WAIT ### Attacker {attackerNumber} requested to enter the semaphore.");
            _semaphore.Wait();
            try
            {
                Console.WriteLine($"ENTER ----> Attacker {attackerNumber} entered the semaphore");
                sw.Restart();
                Thread.Sleep(rnd.Next(2000, 5000));
            }
            finally
            {
                _semaphore.Release();
                Console.WriteLine($"RELEASE <---- Attacker {attackerNumber} released the semaphore.");
            }
        }

        public static void Test()
        {
            Console.WriteLine("SemaphoreSlimTest.Test  Begin");
            _tasks = new Task[_attackers];
            _semaphore = new SemaphoreSlim(MAX_MACHINES);
            Console.WriteLine($"{_semaphore.CurrentCount} attackers are going to be able to enter the semaphore");

            for (int i = 0; i < _attackers; i++)
            {
                _tasks[i] = Task.Factory.StartNew(num =>
                {
                    var attackerNumber = (int)num;
                    for (int j = 0; j < 10; j++)
                    {
                        SimulateAttacks(attackerNumber);
                    }
                }, i);
            }

            var finalTask = Task.Factory.ContinueWhenAll(_tasks, tasks =>
            {
                Task.WaitAll(_tasks);
                Console.WriteLine("The simulation was executed");
                _semaphore.Dispose();
            });

            finalTask.Wait();
        }
    }
}
