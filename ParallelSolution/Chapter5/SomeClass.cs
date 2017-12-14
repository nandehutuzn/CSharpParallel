using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chapter5
{
    class SomeClass
    {
        /*
         *      用于同步的一些类型
         *      
         * 1 Barrier         允许多个任务同步它们不同阶段上的并发工作
         * 2 CountdownEvent  简化了fork和join的情形，表示非常轻量级的同步原语，当计数达到0时触发信号，
         *      新的基于任务的编程模型通过Task实例、延续和Parallel.Invoke可以方便地表达fork-join
         *      。使用Task.WaitAll或TaskFactory.ContinueWhenAll方法要求有一组要等待的Task实例构成
         *      的数组，CountdownEvent不要求对象引用而且可以用于最终随着时间变化的动态数目的任务
         * 3 ManualResetEventSlim  允许很多任务等待直到另一个任务手工发出事件句柄，当预计等待时间很短的时候，
         *      ManualResetEventSlim的性能比对应的重量级ManualResetEventSlim的性能要高
         * 4 SemaphoreSlim  允许限制能够并发访问一个资源或一个资源池的任务的数目，当预计时间很短时，
         *      SemaphoreSlim的性能比对应的重量级Semaphore的性能要高。
         * 5 SpinLock  允许一个任务自旋直到获得一个互斥锁，这样能保证一次只有一个任务能够访问锁定的变量，
         *      对象或区域，当预计等待时间很短时，SpinLock的性能比其他互斥锁的性能要更好，SpinLock为一个结构体
         * 
         * 6 SpinWait  允许一个任务基于自旋的等待，直到指定的条件得到满足，例如一个高级算法可能会使用
         *      SpinWait指定一个超时，从而实现“自旋然后等待”的条件，这种算法称为二阶段等待操作，如果自旋等待
         *      的时间超过一定值而且条件还没有得到满足，那么就进入基于内核的等待
         */
    }
}
