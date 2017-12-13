using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ChapterFour_Concurrent
{
    class Program
    {
        static void Main(string[] args)
        {
            Concurrent.ConcurrentTest();
            Concurrent.ConcurrentTest2();
            Concurrent.ConcurrentTest3();

            Console.ReadKey();
        }
    }
}
