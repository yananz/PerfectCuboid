using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PerfectCuboid
{
    /// <summary>
    /// This class uses a kind of brute to generate all possible Pythagorean triangles.
    /// The algorithm is N^2*LogN. Not quite usable as N is the side of triangles.
    /// </summary>
    class Detector
    {
        // Divide into chunks to improve performance
        private const uint Incremental = 10000;

        private Object _lock = new Object();
        private DataList _B2_plus_C2 = new DataList(false);
        private UInt64[] _allSquaredValue;
        private UInt64 _A;
        public Detector(UInt64 A)
        {
            _A = A;
            GenerateList(_A);
        }

        public void Go()
        {
            DateTime allTimer = DateTime.Now;
            int max_threads = 15;
            Thread[] workingThreads = new Thread[max_threads];

            for (int i = 0; i < max_threads; i++)
            {
                workingThreads[i] = new Thread(Generate_A2_plus_B2);
            }


            for (UInt64 i = _A; i > 0; i-=Incremental)
            {
                DateTime timer = DateTime.Now;

                bool started = false;

                while (!started)
                {
                    int k = 0;
                    while (k < max_threads)
                    {
                        if (workingThreads[k].ThreadState != ThreadState.Running)
                        {
                            // found one and break;
                            //Console.WriteLine("\tThread {0} is not in use, schedule from {1}", k, i);
                            Console.Write(".");
                            workingThreads[k] = new Thread(Generate_A2_plus_B2);
                            workingThreads[k].Start(i);
                            started = true;
                            break;
                        }
                        k++;
                    }

                    if (k >= max_threads)
                    {
                        System.Threading.Thread.Sleep(3000);
                    }
                }

                TimeSpan ts = DateTime.Now.Subtract(timer);
                //Console.WriteLine("\n{0}:\t{1}\n", i, ts.TotalMilliseconds);
            }

            // wait for all threads to complete
            int ii = 0;
            while (ii < max_threads)
            {
                if (workingThreads[ii].ThreadState == ThreadState.Running)
                {
                    ii = -1;
                    System.Threading.Thread.Sleep(3000);
                }
                ii++;
            }

            string filename = string.Format(@"c:\temp\cuboid_{0}_{1}.txt", _A, DateTime.Now.ToString("yyyyMMdd_HHmm"));
            using (System.IO.TextWriter tw = new System.IO.StreamWriter(filename))
            {
                TimeSpan ts = DateTime.Now.Subtract(allTimer);

                tw.WriteLine("Total time for {0}, (ms):(result count):(Increment):(threads) ==> {0}, {1}:{2}", 
                    _A, (UInt64)ts.TotalMilliseconds, _B2_plus_C2._nodes.Count,
                    Incremental, max_threads);
                tw.WriteLine("");

                int index = 1;
                foreach (DataNode dn in _B2_plus_C2._nodes)
                {
                    tw.WriteLine("{0}:\t{1}\t{2}", index++,
                        dn._key, dn._pair);
                }
            }

            //for (int i = 0; i < _B2_plus_C2._nodes.Count; i++)
            //{
            //    if (_B2_plus_C2._nodes[i]._key == 100000)
            //    {
            //        Console.WriteLine("{0}:{1}, {2}:{3}",
            //            _B2_plus_C2._nodes[i]._key,
            //            _B2_plus_C2._nodes[i]._pair,
            //            _B2_plus_C2._nodes[i]._squared,
            //            _B2_plus_C2._nodes[i]._value);
            //    }
            //}
        }

        private void Generate_A2_plus_B2(object m)
        {
            UInt64 nEnd = (UInt64)m;
            UInt64 nStart = nEnd >= Incremental ? nEnd - Incremental : 0;

            // Generate A^2 + B^2 list
            for (UInt64 A = nStart; A < nEnd; A++)
            {
                for (UInt64 B = 0; B < A; B++)
                {
                    UInt64 A2andB2 = _allSquaredValue[A] + _allSquaredValue[B];

                    // Looking for the number that A2andB2 also a square of an integer.
                    // Optimization: A2andB2 must be between max(A, sqart(2)B) < A2andB2 < min(A+B, sqart(2)A)
                    //int left = (int)(A > _allSquaredValue[1, B] ? A : _allSquaredValue[1, B]);
                    //int right = (int)(A+B < _allSquaredValue[0, A] ? A+B : _allSquaredValue[0, A]);
                    //int index = FindSquaredNumberBinarySearch(A2andB2, left, right);
                    int index = FindSquaredNumberBinarySearch(A2andB2, (int)A, (int)(A+B));
                    if (index >= 0)
                    {
                        // valid result
                        //_B2_plus_C2.Add(A+1, B+1, 0);
                    }
                }
            }
        }

        private void GenerateList(UInt64 m)
        {
            UInt64 last = m << 1; // double the size

            // 1.415A, 1.413A, A^2
            _allSquaredValue = new UInt64[last];

            for (UInt64 i = 0; i < last; i++)
            {
                UInt64 k = i + 1;
                _allSquaredValue[i] = k * k;
            }
        }

        private int FindSquaredNumberBinarySearch(UInt64 value, int start, int end)
        {
            while (start <= end)
            {
                int mid = (start + end) >> 1;
                if (_allSquaredValue[mid] == value)
                {
                    // found
                    return mid;
                }

                if (_allSquaredValue[mid] > value)
                {
                    end = mid - 1;
                }
                else
                {
                    start = mid + 1;
                }
            }

            return -1;
        }
    }
}
