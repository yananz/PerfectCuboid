using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PerfectCuboid
{
    class Program
    {
        public enum Action
        {
            Default,
            OutputTextOnly,
            Check,
            EulerBrick,
            EulerBrickAndOutput,
            Testing,
            EulerAll,
        }
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                DisplayUsage();
                return;
            }

            UInt64 m = UInt64.Parse(args[0]);
            UInt64 n = UInt64.Parse(args[1]);

            Action action = Action.Default;

            if (args.Length > 2)
            {
                action = (Action)Enum.Parse(typeof(Action), args[2]);
            }

            //Detector d = new Detector(n);
            //d.Go();

            string filename = string.Format(@"c:\temp\cuboid_Summary_{0}-{1}_{2}.txt", m, n, DateTime.Now.ToString("yyyyMMdd_HHmm"));
            using (TextWriter summaryFile = new StreamWriter(filename))
            {
                DateTime allTimer = DateTime.Now;

                // 1.20 : 
                //   * multi threads generate PT
                //   * multi threads check, if small PT group, using main thread
                // 1.25 :
                //   * binary search for 'k' (C/D) in Finder.
                // 1.30 :
                //   * remove merge to a big file. Just open read all npt files and get the node list
                // 1.31:
                //   * fix output
                // 1.35:
                //   * "Results of computer search for a perfect cuboid" by Robert D. Matson
                //     Odd edge must be great than 2.5x10^13, and even must be greated than 5x10^11
                // 1.40: 1/19/2017 (Not implementeddue to no good algorithm).
                //   * Try to generate all pairs (A, X) to G to save disk.
                //     for each m = 1 to sqrt(G) that N=G-m^2. If N = n^2, then (m, n) is a Euler pair
                //     that X=m^2-n^2 and Y=2mn. consider m>n to make the case even optimized.
                //     But the problem is that this containes prime phythogreans triangles only. For non-PPT,
                //     --we need to cover the odd K factoring case. (how?) so, nothing has been done yet in this version...
                // 1.45: 1/20/2017
                //   * Write file in chunk. (24M each time rather than we do 8 bytes each time now).
                //     * suprisingly found that write is not the issue. The issue is the speed to generate DataNode, and the lock on insert node to sorted set.
                // 1.50: 1/24/2017
                //   * Change to use A^2+B^2+C^2=G^2,hopefully it has much less collection so we can generate much smaller 
                //     data set and check goes quick too.
                //     -- we don't have a good way to generate all collect of 3 sets.
                // 1.51: 1/27/2017
                //   * Try only the valid set that for (x^2, y^2) ==> Z^2, only those valid the last digit:
                //     	(0,0), (0, 1), (0, 4), (0, 5), (0, 6), (0, 9)
                //      (1, 4)
                //      (4, 5), (4, 6)
                //      (6, 9)
                // 1.52: 1/28/2017
                //   * Becaue there is one arm must be divided by 3 in PT, thus there is must one and only one arm in cuboid cannot be divided by 3.
                //     Thus, G cannot divided by 3.
                // 2.0: 1/29/2017
                //   * Generate Euler Brick from PT. "Pythagorean Triangles" by Waclaw Sierpinski, P105, Theorem 15.17
                // 2.01: 2/3/2017
                //   * skip the check if it is end by digit 3 or 7 (it is always odd, so no need to check even numbers)
                // 2.02: 2/4/2017
                //   * improvement in utils to use template class.
                // 2.1: 2/5/2017
                //   * Implement BigUInt to handle any length of data
                // 2.2: 2/5/2017
                //   * uh, just found there is using System.Numerics.BigInteger, which makes everything simple.
                // 2.5: 2/8/2017
                //   * add check (xy)^2 + (yz)^2 + (zx)^2 check (2) check all m,n
                // 2.6: 2/9/2017
                //   * add function to find all Euler Bricks in given lower-upper bound
                // 2.7: 2/9/2017
                //   * the check on sqrt is too slow. 16a^2b^2+C^4 is actually 
                //     m^8+68m^6n^2-122m^4n^4+68m^2n^6+n^8, since m can set to pn, then 
                //     (m^8+68m^6n^2-122m^4n^4+68m^2n^6) / n^8 must be an integer, and the result+1 must be a square
                //     since m and n are relative prime, m must be an even, and n is odd, it then in theory the result
                //     could be integeger, such as 12/3, not another way around.
                // 2.8: 2/12/2017
                //   * Ver 2.7 is actually not fully true. it could have n^8 as factor, or not. Revert back to version 2.2, 
                //     just check 16a^2b^2+c^4. The change in ver2.5 cost too much time. 
                string ver = "2.8";

                string output = string.Format("Version {0}, start at {1}, from {2} to {3}, action:{4}", 
                    ver, allTimer, m, n, action.ToString());
                Utils.Output(summaryFile, output);

                UInt64 foundCount = 0;

                if (action == Action.Testing)
                {
                    Tests.Run();
                }
                else if (action == Action.Check || action == Action.OutputTextOnly)
                {
                    PPT_Formula_m_n ppt = new PPT_Formula_m_n(m, n, summaryFile);
                    UInt64 totalCount = ppt.Generate_all_PT(); // ppt.GenerateAll_Collection3(); // ppt.GeneratePPT();
                    DateTime end_ppt = DateTime.Now;
                    output = string.Format("All NPT n:{0}, duration:{1}, totalColunt={2}, DateTime={3}",
                        n, end_ppt.Subtract(allTimer), totalCount, DateTime.Now);
                    Utils.Output(summaryFile, output);

                    //totalCount = ppt.ExpandPPT_to_NPT();
                    //DateTime end_npt = DateTime.Now;
                    //summaryFile.WriteLine(string.Format("Npt n:{0}, duration:{1}, totalCount={2}", n, end_npt.Subtract(allTimer), totalCount));
                    //summaryFile.Flush();

                    //DataMerger dataMerger = new DataMerger();
                    //string file = dataMerger.Merge();
                    //DateTime end_merge = DateTime.Now;
                    //output = string.Format("Merge Time for {0}: {1}, DateTime={2}", n, end_merge.Subtract(allTimer), DateTime.Now);
                    //Console.WriteLine(output);
                    //summaryFile.WriteLine(output);
                    //summaryFile.Flush();

                    Finder f = new Finder(m, n, summaryFile);
                    if (action == Action.OutputTextOnly)
                    {
                        f.OutputToFileAsReadableString(); // OutputToFileAsReadableString3()
                    }
                    else
                    {
                        foundCount = (UInt64)f.Check();
                    }
                }
                else if (action == Action.EulerAll)
                {
                    EulerBrick eb = new EulerBrick(m, n, summaryFile);
                    foundCount = eb.GenerateAllEulerBricks();
                }
                else
                {
                    EulerBrick eb = new PerfectCuboid.EulerBrick(m, n, summaryFile);
                    foundCount = eb.SeachEulerBrick();
                }

                DateTime end_check = DateTime.Now;
                output = string.Format("Check n:{0}, duration:{1}, foundCount={2}, DateTime={3}",
                    n, end_check.Subtract(allTimer), foundCount, DateTime.Now);
                Utils.Output(summaryFile, output);
            }
        }

        static void DisplayUsage()
        {
            Console.WriteLine("");
            Console.WriteLine("Usage: PerfectCuboid.exe <from number> <top number>");
            Console.WriteLine("");
        }
    }
}
