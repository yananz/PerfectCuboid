using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using BigUInt = System.Numerics.BigInteger;

namespace PerfectCuboid
{
    class Finder
    {
        private DataMerger _reader;
        private DataNode _previous;
        private double SQRT_3 = Math.Sqrt(3) / 3;
        private double SQRT_2 = Math.Sqrt(2) / 2;
        private double SQRT_23 = Math.Sqrt(2 / 3);

        private int _foundTarget = 0;
        private TextWriter _recorder = null;
        private UInt64 _low = 0;
        private UInt64 _high = 0;

        public Finder(UInt64 low, UInt64 high, TextWriter summaryFile)
        {
            _reader = new DataMerger();
            _reader.InitializeQueue();
            _previous = _reader.GetNextDataNode();
            _low = low;
            _high = high;
            _recorder = summaryFile;
        }

        private SortedSet<DataNode>[] GetNextKeyDataListGroup(uint minCount, out int skipped)
        {
            int skippedOne = 0;
            int groupCount = 1000;

            skipped = 0;

            SortedSet<DataNode>[] dataGroups = new SortedSet<DataNode>[groupCount];

            for (int i = 0; i < groupCount; i++)
            {
                skippedOne = 0;
                dataGroups[i] = GetNextKeyDataList(minCount, out skippedOne);
                skipped += skippedOne;
            }

            return dataGroups;
        }
        private SortedSet<DataNode> GetNextKeyDataList(uint minCount, out int skipped)
        {
            SortedSet<DataNode> dnSet = new SortedSet<PerfectCuboid.DataNode>();
            DataNode dn = _previous;

            skipped = 0;

            try
            {
                while (dn != null && dnSet.Count == 0)
                {
                    while (dn != null && _previous._G == dn._G)
                    {
                        dnSet.Add(dn);
                        dn = _reader.GetNextDataNode();
                    }

                    _previous = dn;

                    if (dnSet.Count < minCount || (dnSet.Count > 0 && dnSet.ElementAt(0)._G < _low))
                    {
                        // need at leat minCount, clear this one to get next one
                        skipped += dnSet.Count;
                        dnSet.Clear();
                    }
                }

                return dnSet;
            }
            catch (Exception e)
            {
                Console.WriteLine("Unhandled exception: {0}", e.ToString());
                dnSet.Clear();
                return dnSet;
            }
        }

        private Dictionary<UInt64, DataNode> GenerateSquaredList(SortedSet<DataNode> A, UInt64 A2)
        {
            Dictionary<UInt64, DataNode> squaredList = new Dictionary<ulong, DataNode>();

            foreach (DataNode dn in A)
            {
                UInt64 value = A2 + (UInt64)(dn._B * dn._B);
                squaredList.Add(value, dn);
            }

            return squaredList;
        }

        /// <summary>
        /// This method has problem to check the at most UInt32 (about 4G) since its square will be
        /// the max to UInt64.
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        private void CheckDataListAByUsingSquare(SortedSet<DataNode> A)
        {
            // string result = null;

            if (A.Count >= 3)
            {
                UInt64 A2 = (UInt64)(A.ElementAt(0)._A * A.ElementAt(0)._A);

                // At least 3 pairs required to get (A,B), (A,C), (A,D)
                Dictionary<UInt64, DataNode> squaredList = GenerateSquaredList(A, A2);

                for (int i = 1; i < A.Count - 1; i++)
                {
                    // A[i] is A^2 + B^2
                    for (int j = 0; j < i; j++)
                    {
                        // A[j] is A^2 + C^2

                        // now calculate A^2 + B^2 + C^2, using +- rather than * to reduce the burden of calculation
                        UInt64 A2B2C2 = squaredList.ElementAt(i).Key + squaredList.ElementAt(j).Key - A2;

                        // if A2B2C2 is also in the list, then we found pair A,D ! We got it
                        if (squaredList.ContainsKey(A2B2C2))
                        {
                            Console.WriteLine("wowo... found it! {0}, {1}, {2}",
                                A.ElementAt(i)._A, A.ElementAt(i)._B, A.ElementAt(j)._B);

                            _foundTarget++;
                        }
                    }
                }
            }

            //return result;
        }

        /// <summary>
        /// This is using a way to not calculate square to mkae UInt64 good to UInt64.max/2, which
        /// is roughly 4G*4G/2 = 8GG.
        /// The key is: B^2+C^2=D^2 ==> D^2-B^2=C^2 ==> (D+B)(D-B)=C*C ==> (D+B)/C * (D-B) = C !
        /// Thus convert square to plus, however, downside is that need to test every D.
        /// Considering the number of D is not huge normally few, rarely more than 100.
        /// </summary>
        /// <param name="A"></param>
        /// <returns></returns>
        private string CheckDataListA_by_LongestSide(SortedSet<DataNode> A)
        {
            string result = null;

            if (A.Count >= 3)
            {
                // At least 3 pairs required to get (A,B), (A,C), (A,D)
                for (int i = 1; i < A.Count - 1; i++)
                {
                    UInt64 B = (UInt64)A.ElementAt(i)._B;
                    // A[i] is A^2 + B^2
                    for (int j = 0; j < i; j++)
                    {
                        UInt64 C = (UInt64)A.ElementAt(j)._B;
                        // A[j] is A^2 + C^2
                        for (int k = i + 1; k < A.Count - 1; k++)
                        {
                            // A[k] is A^2 + D^2

                            // now test if (D+B)/C * (D-B) == C ?
                            UInt64 D = (UInt64)A.ElementAt(k)._B;
                            UInt64 D_B = D - B;

                            if (D_B >= C)
                            {
                                // the difference of two sides in a triangle must less then 3rd side
                                break;
                            }

                            UInt64 good = (UInt64)Math.Round(((double)(D + B) / (double)C) * D_B);

                            if (good > C)
                            {
                                // the remaining k(D) will be even larger, so no point to try
                                break;
                            }

                            if (good == C)
                            {
                                result = string.Format("{0}^2+{1}^2+{2}^2 {3}^2",
                                    A.ElementAt(i)._A, B, C, D);

                                Console.WriteLine("wowo... found it!" + result);
                            }
                        }
                    }
                }
            }

            return result;
        }

        public void CheckDataListGroup(object dataGroupsObj)
        {
            SortedSet<DataNode>[] dataGroups = (SortedSet<DataNode>[])dataGroupsObj;
            foreach (SortedSet<DataNode> dg in dataGroups)
            {
                CheckDataListA(dg);
            }
        }

        /// <summary>
        /// Algorithm:
        //        Using longest diagonal G as key to check.

        //        What we are looking for:
        //(1) A^2 + B^2 = D^2
        //(2) A^2 + C^2 = E^2
        //(3) B^2 + C^2 = F^2
        //(4) A^2 + B^2 + C^2 = G^2

        //From #4 above, we also have
        //(5) A^2 + F^2 = G^2
        //(6) B^2 + E^2 = G^2
        //(7) C^2 + D^2 = G^2

        //If we use G as key, the goal is to find the triple inside G group to meet condition(5), (6), and(7).

        //Considering the following restriction to reduce the complexity:
        //suppose A is shortest one side, so 
        //(8) A < B < C, D < E < F, only C and D cannot tell which one is bigger, all others should be in order.
        // Then check A^2 + B^2 + C^ = G^2, that's it!
        // Note: we need to implement UInt128 to store square of UInt64
        // Also, since x^2 can be end with 0,1,4,5,6,9, for A2,B2,C2 combination, result to there is must be at 
        // least one of A,B,C is mutiply of 5. Current, don't know how to apply this condition efficiently.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void CheckDataListA(SortedSet<DataNode> data)
        {
            string result = string.Empty;
            //int binaryThreshold = 3;
            int i, j, k;

            int nodeCount = data.Count;
            if (nodeCount == 0)
            {
                return;
            }

            // The data passed in is a sorted list with all value be the same G
            // So to test all A, B, C, and then D, E, F
            UInt64 G = (UInt64)data.ElementAt(0)._G;
            BigUInt G2 = G * G;

            // Pre-calculate squares for perf
            BigUInt[,] squares = new BigUInt[nodeCount, 2];
            UInt64[,] sqrt2s = new UInt64[nodeCount, 2];
            for (i = 0; i < nodeCount; i++)
            {
                UInt64 key = (UInt64)data.ElementAt(i)._A;
                UInt64 pair = (UInt64)data.ElementAt(i)._B;

                squares[i, 0] = key * key;
                squares[i, 1] = pair * pair;

                sqrt2s[i, 0] = (UInt64)(SQRT_2 * key);
                sqrt2s[i, 1] = (UInt64)(SQRT_2 * pair);
            }

            // for better perf, the relations between those elements are, based on X^2+Y^2=Z^2, X<=0.707Z, Y>=0.707Z
            // note: 0.707 is sqrt(2)/2, and 0.578 is sqrt(3)/3
            // 1. A < 0.707D(orC). ==> A < 0.7D (Not very useful)
            // 2. 0.707C < B < 0.707F ==> B > 0.71C and B < 0.7F
            // 3. C > 0.707F (May not be true, as C or D is not deterministic)
            // 4. Based on A^2+B^2+C^2=G^2, A < 0.578G, (C or D is actually hard to tell)

            // 5. Important that the G must be ODD, and there is only one ODD in a/b/c(or d)

            UInt64 G_Sqrt3 = (UInt64)(SQRT_3 * G);
            UInt64 G_Sqrt23 = (UInt64)(SQRT_23 * G);
            bool aIsOdd = false;
            bool bIsOdd = false;

            // define variable here may help performance
            UInt64 A, B, C, /*C_Sqrt2,*/ D, E, F, F_Sqrt2;
            BigUInt A2, B2, AB2, C2, D2;

            for (i = 0; i < nodeCount - 2; i++)
            { // item [i] is pair of A, F
                A = (UInt64)data.ElementAt(i)._A;
                if (!(A < G_Sqrt3))
                {
                    break;
                }

                aIsOdd = (A & 1) == 1;

                F = (UInt64)data.ElementAt(i)._B;
                A2 = squares[i, 0];

                F_Sqrt2 = sqrt2s[i, 1];

                for (j = i + 1; j < nodeCount - 1; j++)
                { // item [j] is pair of B, E
                    B = (UInt64)data.ElementAt(j)._A;

                    bIsOdd = (B & 1) == 1;

                    if (aIsOdd && bIsOdd)
                    {
                        // cannot have two odds, continue to next
                        continue;
                    }

                    E = (UInt64)data.ElementAt(j)._B;
                    B2 = squares[j, 0];

                    if (!(B < F_Sqrt2))
                    {
                        break;
                    }

                    AB2 = A2 + B2;

                    //if (nodeCount - j < binaryThreshold)
                    //{
                    //    for (k = j + 1; k < nodeCount; k++)
                    //    { // item [k] is pair of C,D
                    //        C_Sqrt2 = sqrt2s[i, 0];
                    //        if (!(B > C_Sqrt2))
                    //        {
                    //            break;
                    //        }

                    //        // now check if crossing 3 sides are Pythagorean triangle or not
                    //        C2 = squares[k, 0];
                    //        if (Utils.BigDataLessThan(AB2, C2))
                    //        {
                    //            // already too small, the following C2 will be even bigger than AB2
                    //            break;
                    //        }
                    //        D2 = squares[k, 1];

                    //        if (Utils.BigDataEqual(AB2, C2) || Utils.BigDataEqual(AB2, D2))
                    //        {
                    //            C = data.ElementAt(k)._key;
                    //            D = data.ElementAt(k)._pair;

                    //            result = string.Format("Found potential... A:{0}, B:{1}, C:{2}, D:{3}, E:{4}, F:{5}, G:{6}",
                    //                A, B, C, D, E, F, G);
                    //            _foundTarget++;
                    //            Console.WriteLine(result);
                    //            _recorder.WriteLine(result);
                    //            _recorder.Flush();
                    //        }
                    //    }
                    //}
                    //else
                    {
                        // binary search k
                        int low = j + 1;
                        int high = nodeCount - 1;
                        int middle = 0;
                        while (low <= high)
                        {
                            middle = (low + high) >> 1;
                            C2 = squares[middle, 0];
                            D2 = squares[middle, 1];
                            if (AB2 == C2 || AB2 == D2)
                            {
                                C = (UInt64)data.ElementAt(middle)._A;
                                D = (UInt64)data.ElementAt(middle)._B;

                                result = string.Format("Found potential... A:{0}, B:{1}, C:{2}, D:{3}, E:{4}, F:{5}, G:{6}",
                                    A, B, C, D, E, F, G);
                                _foundTarget++;
                                Console.WriteLine(result);
                                _recorder.WriteLine(result);
                                _recorder.Flush();
                                break;
                            }
                            else if (AB2 < C2 || D2 < AB2)
                            {
                                high = middle - 1;
                            }
                            else
                            {
                                low = middle + 1;
                            }
                        }
                    }
                }
            }

            //return result;
        }
        public int Check()
        {
            uint minCount = 3;
            UInt64 displayCount = 0;
            UInt64 countTotal = 0;
            int skipped = 0;
            UInt64 skippedTotal = 0;

            //_reader.Reset();

            int maxThreads = 10;
            bool spareThread = false;
            Thread[] threads = new Thread[maxThreads];
            ParameterizedThreadStart start = new ParameterizedThreadStart(CheckDataListGroup);

            for (int i = 0; i < maxThreads; i++)
            {
                threads[i] = new Thread(start);
            }

            SortedSet<DataNode> data_A;
            SortedSet<DataNode>[] data_As;
            UInt64 sectionCount = 0;
            UInt64 currentG = 0;

            while (true)
            {
                spareThread = false;
                for (int i = 0; i < maxThreads; i++)
                {
                    if (!threads[i].IsAlive)
                    {
                        sectionCount = 0;
                        data_As = GetNextKeyDataListGroup(minCount, out skipped);
                        foreach (SortedSet<DataNode> data in data_As)
                        {
                            sectionCount += (UInt64)data.Count;
                        }
                        if (sectionCount == 0)
                        {
                            break;
                        }
                        currentG = (UInt64)data_As.ElementAt(0).ElementAt(0)._G;
                        skippedTotal += (UInt64)skipped;
                        countTotal += sectionCount;

                        threads[i] = new Thread(start);
                        threads[i].Start(data_As);
                        displayCount++;
                        spareThread = true;
                    }
                }

                if (sectionCount == 0)
                {
                    break;
                }

                if (!spareThread)
                {
                    data_A = GetNextKeyDataList(minCount, out skipped);
                    if (data_A.Count == 0)
                    {
                        break;
                    }
                    currentG = (UInt64)data_A.ElementAt(0)._G;
                    skippedTotal += (UInt64)skipped;
                    countTotal += (UInt64)data_A.Count; ;

                    CheckDataListA(data_A);

                    displayCount++;
                    spareThread = true;
                }

                if ((displayCount << 48) == 0)
                {
                    Console.Write(".");
                    if ((displayCount << 44) == 0)
                    {
                        // output every 2^20
                        string output = string.Format("Processed, handled total NPT:{0}, skipped total NPT:{1}, time:{2}, G={3}, found={4}, displayCount={5}",
                            countTotal, skippedTotal, DateTime.Now, currentG, _foundTarget, displayCount);
                        Utils.Output(_recorder, output);
                        _recorder.Flush();
                    }
                }
            }

            spareThread = true;
            while (spareThread)
            {
                spareThread = false;
                for (int i = 0; i < maxThreads; i++)
                {
                    if (threads[i].IsAlive)
                    {
                        spareThread = true;
                        Thread.Sleep(1000);
                        break;
                    }
                }
            }
            return _foundTarget;
        }

        public void OutputToFileAsReadableString()
        {
            int totalCount = 0;
            int maxCount = 0;
            int skipped = 0;
            UInt64 valueOfMax = 0;
            string outputFilename = string.Format(@"c:\temp\cuboid_string_{0}.txt", DateTime.Now.ToString("yyyyMMdd_HHmm"));

            using (TextWriter find_record = new StreamWriter(outputFilename))
            {
                SortedSet<DataNode> data_A = GetNextKeyDataList(1, out skipped);
                while (data_A != null && data_A.Count > 0)
                {
                    totalCount++;
                    if (data_A.Count > maxCount)
                    {
                        maxCount = data_A.Count;
                        valueOfMax = (UInt64)data_A.ElementAt(0)._G;
                    }
                    find_record.WriteLine("----{0}----max count:{1}, value={2}", totalCount, maxCount, valueOfMax);
                    foreach (DataNode dn in data_A)
                    {
                        find_record.WriteLine(dn.ToString());
                    }
                    data_A = GetNextKeyDataList(1, out skipped);
                }
            }
        }
        public UInt64 OutputToFileAsReadableString3()
        {
            return 0;
        }
    }
}
