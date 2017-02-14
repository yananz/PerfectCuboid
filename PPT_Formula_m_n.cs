using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using BigUInt = System.Numerics.BigInteger;

namespace PerfectCuboid
{
    /// <summary>
    /// This class use the m n formula to generate PPT
    /// m^2-n^2, 2mn, m^2+n^2
    /// </summary>
    class PPT_Formula_m_n
    {
        public static UInt64 Min_Odd = 25000000000000;
        public static UInt64 Min_Even = 500000000000;

        private UInt64 _low;
        private UInt64 _high;
        private UInt64 _minValue = 0;
        private UInt64 _maxValue = 0;
        private UInt64[] _squares;
        private DataList[] _dataLists;
        private TextWriter _tw;

        private static int[,] ValidLastDigit = new int[10,10] 
        { 
            {1, 1, 0, 0, 1, 1, 1, 0, 0, 1}, // 0
            {1, 0, 0, 0, 1, 0, 0, 0, 0, 0 }, // 1
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // 2
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // 3
            {1, 1, 0, 0, 0, 1, 1, 0, 0, 0 }, // 4
            {1, 0, 0, 0, 1, 0, 0, 0, 0, 0 }, // 5
            {1, 0, 0, 0, 1, 0, 0, 0, 0, 1 }, // 6
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // 7
            {0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }, // 8
            {1, 0, 0, 0, 0, 0, 1, 0, 0, 0 }, // 9
        };

        private static int _skipped = 0;

        /// <summary>
        /// Generate all PPT from low to high
        /// </summary>
        /// <param name="low"></param>
        /// <param name="high"></param>
        public PPT_Formula_m_n(UInt64 low, UInt64 high, TextWriter tw)
        {
            if (low < 2)
            {
                throw new ArgumentException("Low cannot be less than 2");
            }
            _low = (UInt64)Math.Sqrt(low);
            _high = (UInt64)(Math.Sqrt(high)+1);
            _minValue = low;
            _maxValue = high;
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            Min_Odd = UInt64.Parse(appSettings["Min_Odd"]);
            Min_Even = UInt64.Parse(appSettings["Min_Even"]);
            _tw = tw;
        }

        public static bool CheckKnownConditionOnEdge(UInt64 n)
        {
            bool isOdd = (n & 1) == 1;

            return CheckKnownConditionOnEdge(n, isOdd);
        }
        public static bool CheckKnownConditionOnEdge(UInt64 n, bool isOdd)
        {
            // There is various conditions based on previous investigation

            // "Results of computer search for a perfect cuboid" by Robert D. Matson
            // Odd edge must be great than 2.5x10^13, and even must be greated than 5x10^11
            if (isOdd && n < Min_Odd || n < Min_Even)
            {
                return false;
            }

            return true;
        }

        public UInt64 Generate_all_PT()
        {
            string pattern = string.Format(Path.GetFileName(DataMerger.FilePatternNPT), "*");
            if (!Directory.Exists(DataMerger.PathToFile))
            {
                Directory.CreateDirectory(DataMerger.PathToFile);
            }
            string[] files = Directory.GetFiles(DataMerger.PathToFile, pattern);

            if (files.Length > 0)
            {
                Console.WriteLine("Already have all NPT file exists, count={0}, skip and continue", files.Length);
                return 0;
            }

            DataList.InitStaticData();

            if (_maxValue < _minValue)
            {
                return GenerateTestData();
            }

            UInt64 diff = _maxValue - _minValue;

            //if ((diff << 8) < _maxValue)
            //{
            //    return GeneratePT_enumG();
            //}
            //else
            {
                return GeneratePT_mn2();
            }
        }

        private UInt64 GeneratePT_enumG()
        {
            UInt64 totalCount = 0;

            return totalCount;
        }


        private UInt64 GeneratePT_mn2()
        { 
            int maxThreads = DataList._totalThreads;

            // pre generate squares for perf.
            _squares = new UInt64[_high];
            for (UInt64 i = 0; i < _high; i++)
            {
                _squares[i] = (i + 1) * (i + 1);
            }

            Thread[] threads = new Thread[maxThreads];
            ParameterizedThreadStart start = new ParameterizedThreadStart(GeneratePTThread);
            _dataLists = new DataList[maxThreads];
            for (int i = 0; i < maxThreads; i++)
            {
                threads[i] = new Thread(start);
                _dataLists[i] = new DataList();
            }

            // for each m
            UInt64 m = 1;
            UInt64 scope = 10;
            UInt64 displayScope = _high >> 4;
            UInt64 displayThreshold = displayScope;
            int displayCount = 0;
            bool runningThread = false;
            while (m < _high)
            {
                runningThread = false;
                for (int i = 0; i < maxThreads; i++)
                {
                    if (!threads[i].IsAlive)
                    {
                        ThreadParameters tps = new ThreadParameters();
                        tps.Low = m+1;
                        m += scope;
                        tps.High = m;
                        tps.ThreadID = i;
                        tps.dl = _dataLists[i];

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
                for (int i = 0; i < maxThreads; i++)
                {
                    if (threads[i].IsAlive)
                    {
                        runningThread = true;
                        break;
                    }
                }

                Thread.Sleep(3000);
            }

            // last part
            lock (DataList._lock)
            {
                DataList.Output(_dataLists, _tw);
            }

            Utils.Output(_tw, string.Format("Total skipped = {0}", _skipped));
            return DataList._totalCount;
        }

        private void GeneratePTThread(object obj)
        {
            ThreadParameters parameters = (ThreadParameters)obj;

            UInt64 low = parameters.Low;
            UInt64 high = parameters.High > _high ? _high: parameters.High;
            DataList dl = parameters.dl;
            for (UInt64 m = low; m <= high; m++)
            {
                // calculate each n

                // only m and n is not the same odd/even then it generates PPT
                //if ((m & 1) != (n & 1))

                for (UInt64 n = 1 + (m & 1); n < m; n += 2)
                {
                    UInt64 m2 = _squares[m - 1];
                    UInt64 n2 = _squares[n - 1];
                    UInt64 m2_minus_n2 = m2 - n2;
                    UInt64 mn2 = (m * n) << 1;
                    UInt64 m2_plus_n2 = m2 + n2;

                    if (m2_plus_n2 > _maxValue)
                    {
                        break;
                    }

                    UInt64 k = (UInt64)(_maxValue / m2_plus_n2);

                    if (k == 0)
                    {
                        break;
                    }

                    // m2_plus_n2_k cannot be even number, thus do nothing if k is even
                    if ((k & 1) == 0)
                    {
                        k--;
                    }

                    UInt64 m2_plus_n2_k = m2_plus_n2 * k;
                    UInt64 m2_minus_n2_k = m2_minus_n2 * k;
                    UInt64 mn2_k = mn2 * k;
                    UInt64 m2_plus_n2_2 = m2_plus_n2 << 1;
                    UInt64 m2_minus_n2_2 = m2_minus_n2 << 1;
                    UInt64 mn2_2 = mn2 << 1;
                    int Gr = (int)(m2_plus_n2 % 3);

                    while ((Gr != 0) && k > 0 && m2_plus_n2_k > _minValue)
                    {
                        // G is not divisable by 3
                        if (((k % 3) != 0) &&
                            PPT_Formula_m_n.CheckKnownConditionOnEdge(m2_minus_n2_k) &&
                            PPT_Formula_m_n.CheckKnownConditionOnEdge(mn2_k))
                        {
                            //UInt128 X2 = Utils.BigDataSquare(m2_minus_n2_k);
                            //UInt128 Y2 = Utils.BigDataSquare(mn2_k);

                            UInt64 X2 = m2_minus_n2_k * m2_minus_n2_k;
                            UInt64 Y2 = mn2_k * mn2_k;

                            int Xr = (int)(X2 % 10);
                            int Yr = (int)(Y2 % 10);

                            if (ValidLastDigit[Xr, Yr] == 1)
                            {
                                if (DataList._readyToWriteCount >= DataList._countInFile)
                                {
                                    lock (DataList._lock)
                                    {
                                        if (DataList._readyToWriteCount >= DataList._countInFile)
                                        {
                                            DataList.Output(_dataLists, _tw);
                                        }
                                    }
                                }
                                dl.Add(m2_minus_n2_k, mn2_k, m2_plus_n2_k);
                            }
                            else
                            {
                                System.Threading.Interlocked.Increment(ref _skipped);
                            }
                        }
                        if (k <= 1)
                        {
                            break;
                        }
                        k--;
                        k--;

                        m2_plus_n2_k = m2_plus_n2_k - m2_plus_n2_2;
                        m2_minus_n2_k = m2_minus_n2_k - m2_minus_n2_2;
                        mn2_k = mn2_k - mn2_2;
                    }
                }
            }
        }

        public UInt64 GenerateTestData()
        {
            _dataLists = new DataList[2];
            _dataLists[0] = new DataList();
            _dataLists[1] = new DataList();

            _dataLists[0].Add(44, 267, 271);
            _dataLists[0].Add(125, 240, 271);

            _dataLists[0].Add(44, 267, 270);
            _dataLists[1].Add(117, 244, 270);
            _dataLists[0].Add(125, 120, 270);

            _dataLists[1].Add(44, 267, 272);
            _dataLists[0].Add(117, 244, 272);
            _dataLists[1].Add(126, 239, 272);

            DataList.Output(_dataLists, _tw);

            _dataLists[0].Add(117, 244, 271);
            _dataLists[1].Add(125, 240, 271);

            _dataLists[1].Add(49, 267, 271);
            _dataLists[0].Add(220, 240, 271);

            DataList.Output(_dataLists, _tw);

            return (UInt64)DataList._totalCount;

        }

        /// <summary>
        /// Find all list of A^2 + B^2 + C^2 = G^2, using similar method
        /// A = X^2 - Y^2 - Z^2
        /// B = 2XY
        /// C = 2XZ
        /// G = X^2 + Y^2 +Z^2
        /// </summary>
        /// <returns></returns>
        //public UInt64 GenerateAll_Collection3()
        //{
        //    string pattern = string.Format(Path.GetFileName(DataMerger.FilePatternNPT), "*");
        //    if (!Directory.Exists(DataMerger.PathToFile))
        //    {
        //        Directory.CreateDirectory(DataMerger.PathToFile);
        //    }
        //    string[] files = Directory.GetFiles(DataMerger.PathToFile, pattern);

        //    if (files.Length > 0)
        //    {
        //        Console.WriteLine("Already have all NPT file exists, count={0}, skip and continue", files.Length);
        //        return 0;
        //    }

        //    DataList3.InitStaticData();

        //    if (_maxValue < _minValue)
        //    {
        //        return GenerateTestData3();
        //    }

        //    //UInt64 diff = _maxValue - _minValue;

        //    return GenerateAll_Collection3_Brute();
        //}

        private int FindValue(BigUInt[] array, BigUInt value)
        {
            int high = array.Length;
            int low = 0;
            int mid;

            while (low < high)
            {
                mid = (low + high) >> 1;
                if (array[mid] == value)
                {
                    return mid;
                }

                if (array[mid] < value)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return -1;
        }

        //private UInt64 GenerateAll_Collection3_Brute()
        //{
        //    DataList3 dataList = new DataList3();

        //    BigUInt[] squares = new BigUInt[_maxValue];
        //    BigUInt limit = new BigUInt(_maxValue).Square();
        //    for (UInt64 i = 0; i < _maxValue; i++)
        //    {
        //        squares[i] = new BigUInt(i + 1).Square();
        //    }

        //    for (UInt64 i = 3; i < _maxValue; i++)
        //    {
        //        for (UInt64 j = 2; j < i; j++)
        //        {
        //            for (UInt64 k = 1; k < j; k++)
        //            {
        //                BigUInt value = squares[i - 1] + squares[j - 1] + squares[k - 1];

        //                int index = FindValue(squares, value);
        //                if (index >= 0)
        //                {
        //                    DataNode3 dn = new PerfectCuboid.DataNode3(i, j, k, (UInt64)(index + 1));
        //                    dataList.Add(dn);
        //                }
        //            }
        //        }
        //    }

        //    DataList3[] t = new DataList3[1];
        //    t[0] = dataList;

        //    foreach (DataNode3 dn  in dataList._nodes)
        //    {
        //        Console.WriteLine(dn.ToString());
        //    }

        //    DataList3.Output(t);

        //    return (UInt64)dataList._nodes.Count;
        //}

        public UInt64 GenerateTestData3()
        {
            return 0;
        }
    }
}
