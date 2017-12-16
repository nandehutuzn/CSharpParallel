using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chapter5
{
    /// <summary>
    /// 通过屏障同步很多带有多个阶段的任务
    /// </summary>
    class BarrierTest
    {
        private static int _participants = Environment.ProcessorCount;
        private static Task[] _tasks;
        private static Barrier _barrier;
        private const int TIMEOUT = 2000;

        private static void CreatePlanets(int participantNum)
        {
            Console.WriteLine($"Create Planets. Participant. # {participantNum}");
        }

        private static void CreatePlanets2(int participantNum)
        {
            Console.WriteLine($"Create Planets. Participant. # {participantNum}");

            if (participantNum == 0)
            {
                SpinWait.SpinUntil(() => _barrier.ParticipantsRemaining == 0, TIMEOUT * 3);
            }
        }

        private static void CreateStars(int participantNum)
        {
            Console.WriteLine($"Create Stars. Participant # {participantNum}");
        }

        private static void CheckCollisionsBetweenPlanets(int participantNum)
        {
            Console.WriteLine($"Checking collections between planets. Participant # {participantNum}");
        }

        private static void CheckCollisionsBetweenStars(int participantNum)
        {
            Console.WriteLine($"Checking collisions between stars. Participant # {participantNum}");
        }

        private static void RenderCollisions(int participantNum)
        {
            Console.WriteLine($"Rendering collisions.  Participant # {participantNum}");
        }

        public static void  Test()
        {
            Console.WriteLine("BarrierTest.Test   Begin");
            _tasks = new Task[_participants];
            _barrier = new Barrier(_participants, barrier => Console.WriteLine($"Current phase: {barrier.CurrentPhaseNumber}"));
            
            for (int i = 0; i < _participants; i++)
            {
                _tasks[i] = Task.Factory.StartNew(num =>
                {
                    var participantNumber = (int)num;
                    for (int j = 0; j < 10; j++)
                    {
                        CreatePlanets(participantNumber);
                        _barrier.SignalAndWait();
                        CreateStars(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenPlanets(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenStars(participantNumber);
                        _barrier.SignalAndWait();
                        RenderCollisions(participantNumber);
                        _barrier.SignalAndWait();
                    }
                }, i);
            }

            var finalTask = Task.Factory.ContinueWhenAll(_tasks, tasks =>
            {
                Task.WaitAll(_tasks);
                Console.WriteLine("All the phases were executed.");
                _barrier.Dispose();
            });

            finalTask.Wait();
        }

        public static void TestThrowException()
        {
            Console.WriteLine("BarrierTest.TestThrowException   Begin");
            _tasks = new Task[_participants];
            _barrier = new Barrier(_participants, barrier =>
            {
                Console.WriteLine($"Current phase: {barrier.CurrentPhaseNumber}");
                if (barrier.CurrentPhaseNumber == 10)
                {
                    throw new InvalidOperationException("No more phases allowed");
                }
            });

            for (int i = 0; i < _participants; i++)
            {
                _tasks[i] = Task.Factory.StartNew(num =>
                {
                    var participantNumber = (int)num;
                    for (int j = 0; j < 10; j++)
                    {
                        CreatePlanets(participantNumber);
                        try
                        {
                            _barrier.SignalAndWait();
                        }
                        catch (BarrierPostPhaseException bppex)
                        {
                            Console.WriteLine(bppex.InnerException.Message);
                            break;
                        }
                        CreateStars(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenPlanets(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenStars(participantNumber);
                        _barrier.SignalAndWait();
                        RenderCollisions(participantNumber);
                        _barrier.SignalAndWait();
                    }
                }, i);
            }

            var finalTask = Task.Factory.ContinueWhenAll(_tasks, tasks =>
            {
                Task.WaitAll(_tasks);
                Console.WriteLine("All the phases were executed.");
                _barrier.Dispose();
            });

            finalTask.Wait();
        }

        public static void TestTimeout()
        {
            Console.WriteLine("BarrierTest.TestTimeout   Begin");
            var cts = new System.Threading.CancellationTokenSource();
            var ct = cts.Token;
            _tasks = new Task[_participants];
            _barrier = new Barrier(_participants, barrier => Console.WriteLine($"Current phase: {barrier.CurrentPhaseNumber}"));

            for (int i = 0; i < _participants; i++)
            {
                _tasks[i] = Task.Factory.StartNew(num =>
                {
                    var participantNumber = (int)num;
                    for (int j = 0; j < 10; j++)
                    {
                        CreatePlanets2(participantNumber);
                        if (!_barrier.SignalAndWait(TIMEOUT))
                        {
                            Console.WriteLine($"Participants are requiring more than {TIMEOUT} seconds to reach the barrier");
                            throw new OperationCanceledException($"Participants are requiring more than {TIMEOUT} seconds to reach the barrier at the Phase # {_barrier.CurrentPhaseNumber}", ct);
                        }
                        CreateStars(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenPlanets(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenStars(participantNumber);
                        _barrier.SignalAndWait();
                        RenderCollisions(participantNumber);
                        _barrier.SignalAndWait();
                    }
                }, i, ct);
            }

            var finalTask = Task.Factory.ContinueWhenAll(_tasks, tasks =>
            {
                Task.WaitAll(_tasks);
                Console.WriteLine("All the phases were executed.");
                //_barrier.Dispose();
            }, ct);

            try
            {
                if (!finalTask.Wait(TIMEOUT * 2))
                {
                    bool faulted = false;
                    for (int t = 0; t < _participants; t++)
                    {
                        if (_tasks[t].Status != TaskStatus.RanToCompletion)
                        {
                            faulted = true;
                            if (_tasks[t].Status == TaskStatus.Faulted)
                            {
                                foreach (var innerEx in _tasks[t].Exception.InnerExceptions)
                                    Console.WriteLine(innerEx.Message);
                            }
                        }
                    }

                    if (faulted)
                    {
                        Console.WriteLine("The phases failed their execution");
                    }
                    else
                        Console.WriteLine("All the phases were executed");
                }
            }
            catch (AggregateException ex)
            {
                foreach (var innerEx in ex.InnerExceptions)
                {
                    Console.WriteLine(innerEx.Message);
                }
                Console.WriteLine("The phases failed their execution");
            }
            finally
            {
                _barrier.Dispose();
            }
        }

        /// <summary>
        /// 基于自旋的等待
        /// </summary>
        public static void BarrierAndSpinLockTest()
        {
            Console.WriteLine("BarrierAndSpinLockTest Begin");
            _tasks = new Task[_participants];
            _barrier = new Barrier(_participants, barrier => Console.WriteLine($"Current phase: {barrier.CurrentPhaseNumber}"));

            //不要将SpinLock声明为自读字段，如果这么做的话，会导致每次调用这个字段都会返回
            //SpinLock的一个新副本，而不是原始的那个，所有对Enter的方法调用都能成功获得锁
            var sl = new SpinLock(false);
            var sb = new StringBuilder();

            for (int i = 0; i < _participants; i++)
            {
                _tasks[i] = Task.Factory.StartNew(num =>
                {
                    var participantNumber = (int)num;
                    for (int j = 0; j < 10; j++)
                    {
                        CreatePlanets(participantNumber);
                        _barrier.SignalAndWait();
                        CreateStars(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenPlanets(participantNumber);
                        _barrier.SignalAndWait();
                        CheckCollisionsBetweenStars(participantNumber);
                        _barrier.SignalAndWait();
                        RenderCollisions(participantNumber);
                        _barrier.SignalAndWait();

                        var logLine = $"Time: {DateTime.Now.TimeOfDay}, Phase: {_barrier.CurrentPhaseNumber}, Participant: {participantNumber}, Phase completed OK\n";
                        bool lockTaken = false;
                        try
                        {
                            //sl.Enter(ref lockTaken);
                            //sb.Append(logLine);

                            sl.TryEnter(2000, ref lockTaken);
                            if (!lockTaken)
                            {
                                Console.WriteLine($"Lock timeout for participant: {participantNumber}");
                                throw new TimeoutException($"Participant are requiring more than {2000} seconds to acquire the lock at the parse # {_barrier.CurrentPhaseNumber}");
                            }
                            sb.Append(logLine);
                        }
                        finally
                        {
                            if (lockTaken)
                                sl.Exit(false);
                        }
                    }
                }, i);
            }

            var finalTask = Task.Factory.ContinueWhenAll(_tasks, tasks =>
            {
                Console.WriteLine($"Enter ContinueWhenAll Time: {DateTime.Now.TimeOfDay}");
                Task.WaitAll(_tasks);
                Console.WriteLine($"All the phase were executed. Time: {DateTime.Now.TimeOfDay}");
                Console.WriteLine(sb);
                _barrier.Dispose();
            });

            finalTask.Wait();
        }
    }
}
