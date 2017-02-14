using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Numerics;
using System.Linq;
using System.Text;
using System.Threading;

namespace PerfectCuboid
{
    //For any PT, we can generate a Euler Brick, per "Pythagorean Triangles" by Waclaw Sierpinski, P105, Theorem 15.17: For a PT(a, b, c), we can have Euler Brick:
    //X = a^2 * |4b^2 - c^2|
    //Y = b^2 * |4a^2 - c^2|
    //Z = 4abc

    //Only need to check PT for primitive perfect cuboid, if there is any.
    class EulerBrick
    {
        private object _locker = new object();
        private UInt64 _low;
        private UInt64 _high;
        private TextWriter _tw;

        private Int64 _totalSearchCount = 0;
        private Int64 _totalPerfectCuboidFound = 0;
        private int _totalThreads = 10;
        private DataSet _maxSearchedCandidate = new DataSet();

        public EulerBrick(UInt64 low, UInt64 high, TextWriter tw)
        {
            _low = low;
            _high = high;
            _tw = tw;
            _maxSearchedCandidate.check = 0;
        }

        public UInt64 SeachEulerBrick()
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            _totalThreads = int.Parse(appSettings["Threads"]);

            Thread[] threads = new Thread[_totalThreads];
            ParameterizedThreadStart start = new ParameterizedThreadStart(SearchEulerBrickThread);
            for (int i = 0; i < _totalThreads; i++)
            {
                threads[i] = new Thread(start);
            }

            // for each m
            UInt64 m = _low;
            UInt64 scope = 100;
            UInt64 displayScope = _high - _low >> 6; // display 64 chunks for progress
            UInt64 displayThreshold = _low + displayScope;
            int displayCount = 0;
            bool runningThread = false;
            while (m < _high)
            {
                runningThread = false;
                for (int i = 0; i < _totalThreads; i++)
                {
                    if (!threads[i].IsAlive)
                    {
                        ThreadParameters tps = new ThreadParameters();
                        tps.Low = m + 1;
                        m += scope;
                        tps.High = m;
                        tps.ThreadID = i;
                        tps.dl = null;

                        threads[i] = new Thread(start);
                        threads[i].Start(tps);

                        runningThread = true;
                        if (m >= _high)
                        {
                            break;
                        }
                    }
                }

                if (m > displayThreshold)
                {
                    displayCount++;
                    Utils.Output(_tw, string.Format(".Calculated {0}: m/_high = {1}/{2}. DateTime={3}", displayCount, m, _high, DateTime.Now));
                    displayThreshold += displayScope;
                }

                if (!runningThread)
                {
                    Thread.Sleep(500);
                }
            }

            runningThread = true;
            while (runningThread)
            {
                runningThread = false;
                for (int i = 0; i < _totalThreads; i++)
                {
                    if (threads[i].IsAlive)
                    {
                        runningThread = true;
                        break;
                    }
                }

                Thread.Sleep(3000);
            }

            Utils.Output(_tw, string.Format("time:{0} checked:{1} found:{2}, maxSearched:{3}",
                DateTime.Now, _totalSearchCount, _totalPerfectCuboidFound, _maxSearchedCandidate));

            return (UInt64)_totalPerfectCuboidFound;
        }

        private void SearchEulerBrickThread(object obj)
        {
            ThreadParameters parameters = (ThreadParameters)obj;

            // Low to high should be UInt32, which covers 2*2^32*2^32 = 2*264
            UInt64 from = parameters.Low; // + (parameters.Low & 0x1);
            UInt64 to = parameters.High > _high ? _high : parameters.High;
            for (UInt64 m = from; m <= to; m++) // m must be even
            {
                // calculate each n

                // only m and n is not the same odd/even then it generates PPT
                //if ((m & 1) != (n & 1))

                for (UInt64 n = 1 + (m & 1); n < m; n += 2)
                {
                    DataSet ds = new DataSet(m, n);
                    //DataSet1 ds = new DataSet1(m, n);

                    // if ((ds.c & 0x1)==1 && ds.check.IsOdd()) // C, G, and Check are always odd here
                    {
                        if (ds.DataCheck())
                        {
                            System.Threading.Interlocked.Increment(ref _totalPerfectCuboidFound);
                            Utils.Output(_tw, string.Format("Haha: found one! total found: {0}, ds:{1}",
                                _totalPerfectCuboidFound, ds));
                            Console.ReadLine();
                        }
                    }

                    if (ds.valid && ds.check > _maxSearchedCandidate.check)
                    {
                        lock (_locker)
                        {
                            if (ds.valid && ds.check > _maxSearchedCandidate.check)
                            {
                                _maxSearchedCandidate = ds;
                            }
                        }
                    }

                    // interlock to increase _totalSearchCount and _totalFoundCount, if there is any.
                    System.Threading.Interlocked.Increment(ref _totalSearchCount);
                    if ((_totalSearchCount & 0xffffffff) == 0)
                    {
                        Utils.Output(_tw, string.Format("time:{0} checked:{1} found:{2}, maxSearched:{3}",
                            DateTime.Now, _totalSearchCount, _totalPerfectCuboidFound, _maxSearchedCandidate));
                    }
                    else if ((_totalSearchCount & 0xfffffff) == 0)
                    {
                        Console.Write(".");
                    }
                }
            }
        }

        public UInt64 GenerateAllEulerBricks()
        {
            UInt64 found = 0;
            for (UInt64 i = _low; i < _high; i++)
            {
                for (UInt64 j = _low; j < i; j++)
                {
                    for (UInt64 k = _low; k < j; k++)
                    {
                        BigInteger i2 = i * i;
                        BigInteger j2 = j * j;
                        BigInteger k2 = k * k;
                        if (Utils.IsPerfectSquare(i2+j2) && Utils.IsPerfectSquare(j2+k2) && Utils.IsPerfectSquare(k2+i2))
                        {
                            BigInteger G = i2 + j2 + k2;
                            found++;
                            Utils.Output(_tw, string.Format("{0}:\t{1}^2+{2}^2+{3}^2={4}", found, i, j, k, G));
                        }
                    }
                }
            }

            return found;
        }
    }
}
