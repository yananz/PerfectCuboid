using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using BigUInt = System.Numerics.BigInteger;

namespace PerfectCuboid
{
    class Tests
    {
        public static bool _AllPassed = true;
        public static void Run()
        {
            TestBigDataMultiply(new BigUInt(11), new BigUInt(12), new BigUInt(132));
            TestBigDataMultiply(new BigUInt(0), new BigUInt(1), new BigUInt(0));
            TestBigDataMultiply(new BigUInt(0xffff), new BigUInt(0xfffe), new BigUInt((UInt64)0xffff * (UInt64)0xfffe));
            TestBigDataMultiply(new BigUInt(UInt32.MaxValue), new BigUInt(UInt32.MaxValue), new BigUInt((UInt64)UInt32.MaxValue * (UInt64)UInt32.MaxValue));
            TestBigDataMultiply(new BigUInt(5), new BigUInt(5), new BigUInt(5 * 5));
            TestBigDataMultiply(new BigUInt((UInt64)UInt32.MaxValue + 1), new BigUInt((UInt64)UInt32.MaxValue + 1), BigInteger.Pow(2, 64));

            Console.WriteLine("    ");

            // 	A: 200066880^2 = 40026756472934400, 
            //  B: 133407891 ^ 2 = 17797665381067881
            //  A ^ 2 + B ^ 2 = 40026756472934400 + 17797665381067881 = 57824421854002281 = 240467091 ^ 2
            BigUInt k = 1;
            BigUInt A = 200066880;
            BigUInt B = 133407891;
            BigUInt C = 240467091;
            TestIsOdd(k, true);
            TestIsOdd(A, false);
            TestIsOdd(B, true);
            TestIsOdd(C, true);
            for (UInt64 i = 1; i < 10; i++)
            {
                k = k * i;
                BigUInt Ak = A * k;
                BigUInt A2 = Ak * Ak; // Utils.BigDataMutiply(A * k, A * k);
                BigUInt Bk = B * k;
                BigUInt B2 = Bk * Bk; // Utils.BigDataMutiply(B * k, B * k);
                BigUInt sumA2B2 = A2 + B2; //Utils.BigDataAdd(A2, B2);
                TestBigDataMultiply(C*k, C*k, sumA2B2);
                bool iIsOdd = i < 2;
                TestIsOdd(k, iIsOdd);
                TestIsOdd(Ak, !A.IsEven && iIsOdd);
                TestIsOdd(A2, !A.IsEven && iIsOdd);
                TestIsOdd(Bk, !Bk.IsEven && iIsOdd);
                TestIsOdd(B2, !Bk.IsEven && iIsOdd);
            }

            Console.WriteLine("    ");

            for (UInt64 i = 11; i < 100; i++)
            {
                k = k * new BigUInt(i);
                BigUInt Aak = A * k;
                BigUInt Aa2 = Aak * Aak; // Utils.BigDataMutiply(A * k, A * k);
                BigUInt Bbk = B * k;
                BigUInt Bb2 = Bbk * Bbk; // Utils.BigDataMutiply(B * k, B * k);
                BigUInt sumAa2Bb2 = Aa2 + Bb2; //Utils.BigDataAdd(A2, B2);
                BigUInt Cck = C * k;
                TestBigDataMultiply(Cck, Cck, sumAa2Bb2);
                TestIsPerfectSquare(sumAa2Bb2, true);
            }

            Console.WriteLine("    ");
            TestIsPerfectSquare(new BigUInt(356* 356), true);
            TestIsPerfectSquare(new BigUInt((UInt64)2555559 * (UInt64)33334343), false);
            TestIsPerfectSquare(new BigUInt(3), false);
            TestIsPerfectSquare(new BigUInt(4), true);
            TestIsPerfectSquare(new BigUInt(23432 * 23432), true);
            TestIsPerfectSquare(new BigUInt(23456) * new BigUInt(345334), false);
            TestIsPerfectSquare(new BigUInt((UInt64)11223344556677) * new BigUInt((UInt64)11223344556677), true);
            TestIsPerfectSquare(new BigUInt((UInt64)11223344556677) * new BigUInt((UInt64)11223344556678), false);
            BigUInt Cc = C;
            TestIsPerfectSquare(Cc*Cc, true);
            TestIsPerfectSquare(k * A, false);
            Cc = Cc * Cc;
            TestIsPerfectSquare(Cc, true);
            Cc = Cc * Cc;
            TestIsPerfectSquare(Cc, true);
            Cc = Cc * Cc;
            TestIsPerfectSquare(Cc, true);
            TestIsOdd(Cc, !C.IsEven);

            Console.WriteLine("    ");
            TestMinus(new BigUInt(2), new BigUInt(1), new BigUInt(1));
            TestMinus(new BigUInt(2), new BigUInt(3), new BigUInt(-1));
            TestMinus(new BigUInt((UInt64)11223344556677) * new BigUInt((UInt64)11223344556678), new BigUInt((UInt64)11223344556677) * new BigUInt((UInt64)11223344556677), new BigUInt((UInt64)11223344556677));
            TestMinus(new BigUInt(1), new BigUInt(3), new BigUInt(-2));

            Console.WriteLine("    ");
            TestLastDigital(new BigUInt(1234567), 7);
            TestLastDigital(new BigUInt(11223344), 4);

            Console.WriteLine("    ");
            TestEulerBrickFiltersAndSearch();

            DataNode dn = new DataNode(4, 3, 5);
            Console.WriteLine("dn = {0}", dn.ToString());
            dn = new DataNode(7, 9, 8, 12);
            Console.WriteLine("dn = {0}", dn.ToString());
            DataSet ds = new DataSet(4, 3);
            Console.WriteLine("ds = {0}", ds.ToString());

            Console.WriteLine("    ");

            if (_AllPassed)
            {
                Console.WriteLine("All passed !!!!!!!!!!");
            }
            else
            {
                Console.WriteLine("There are failures ..............");
            }

            Console.ReadLine();
        }

        private static void TestBigDataMultiply(BigUInt a, BigUInt b, BigUInt expect)
        {
            BigUInt result = a * b; 
            bool rel = result == expect; // Utils.BigDataEqual(result, expect);
            _AllPassed = _AllPassed && rel;
            Console.WriteLine("{0} : Test TestBigDataMultiply: {1}, {2}, {3}:{4}", rel ? "Succeeded" : "Failed", a, b, result, expect);
        }

        private static void TestIsPerfectSquare(BigUInt N, bool expect)
        {
            bool result = Utils.IsPerfectSquare(N);
            _AllPassed = _AllPassed && (result == expect);
            Console.WriteLine("{0} : Test TestIsPerfectSquare: {1}, {2}:{3}", result == expect ? "Succeeded" : "Failed", N, result, expect);
        }

        private static void TestMinus(BigUInt a, BigUInt b, BigUInt expect)
        {
            BigUInt result = a - b;
            _AllPassed = _AllPassed && (result == expect);
            Console.WriteLine("{0} : Test TestMinus: {1}-{2}={3}, expect:{4}", result == expect ? "Succeeded" : "Failed", a, b, result, expect);
        }

        private static void TestIsOdd(BigUInt N, bool expect)
        {
            bool result = !N.IsEven;
            _AllPassed = _AllPassed && (result == expect);
            Console.WriteLine("{0} : Test TestIsOdd: {1}, {2}:{3}", result == expect ? "Succeeded" : "Failed", N, result, expect);
        }

        private static void TestLastDigital(BigUInt N, UInt64 expect)
        {
            UInt64 result = (UInt64)(N % 10);
            _AllPassed = _AllPassed && (result == expect);
            Console.WriteLine("{0} : Test TestIsOdd: {1}, {2}:{3}", result == expect ? "Succeeded" : "Failed", N, result, expect);
        }

        private static void TestEulerBrickFiltersAndSearch()
        {
            BigInteger x = 44;
            BigInteger y = 117;
            BigInteger z = 240;

            TestCondition(
                "Known Euler brick passes necessary perfect-cuboid filters",
                PerfectCuboidMath.PassesNecessaryEdgeFilters(x, y, z));

            BigInteger spaceDiagonalSquared = x * x + y * y + z * z;
            TestCondition(
                "Known Euler brick has no integer space diagonal",
                !Utils.IsPerfectSquare(spaceDiagonalSquared));

            DataSet formulaBrick = new DataSet(2, 1);
            TestCondition(
                "Formula m=2,n=1 generates the 44,117,240 Euler brick",
                formulaBrick.eb._A == 44 &&
                formulaBrick.eb._B == 117 &&
                formulaBrick.eb._C == 240);

            TestCondition(
                "Square residue sieve accepts an actual square",
                PerfectCuboidMath.PassesSquareResidueSieve(new BigInteger(1234567) * 1234567));

            TestCondition(
                "Exact integer square root handles values above UInt64",
                PerfectCuboidMath.IntegerSquareRoot(BigInteger.Pow(2, 160)) == BigInteger.Pow(2, 80));

            CompleteEulerSearchResult result =
                new CompleteEulerBrickSearch(1, 240, null).Search();
            TestCondition(
                "Complete Euler graph finds exactly the known 44,117,240 brick in range",
                result.PrimitiveEulerBricks == 1);
            TestCondition(
                "No perfect cuboid is reported in the small regression range",
                result.PerfectCuboids == 0);

            TestCompleteEulerShards();
            TestCompleteEulerCheckpointResume();
        }

        private static void TestCompleteEulerShards()
        {
            CompleteEulerSearchOptions firstOptions = new CompleteEulerSearchOptions();
            firstOptions.SmallestEdgeFrom = 1;
            firstOptions.SmallestEdgeTo = 100;
            firstOptions.ProgressSeconds = 3600;

            CompleteEulerSearchOptions secondOptions = new CompleteEulerSearchOptions();
            secondOptions.SmallestEdgeFrom = 101;
            secondOptions.SmallestEdgeTo = 240;
            secondOptions.ProgressSeconds = 3600;

            CompleteEulerSearchResult first =
                new CompleteEulerBrickSearch(1, 240, null, firstOptions).Search();
            CompleteEulerSearchResult second =
                new CompleteEulerBrickSearch(1, 240, null, secondOptions).Search();

            TestCondition(
                "Disjoint smallest-edge shards equal the unsharded Euler-brick count",
                first.PrimitiveEulerBricks + second.PrimitiveEulerBricks == 1);
        }

        private static void TestCompleteEulerCheckpointResume()
        {
            string stem = Path.Combine(
                Path.GetTempPath(),
                "PerfectCuboid-test-" + Guid.NewGuid().ToString("N"));
            string checkpointPath = stem + ".checkpoint";
            string resultsPath = stem + ".results.log";

            try
            {
                CompleteEulerSearchOptions options = new CompleteEulerSearchOptions();
                options.SmallestEdgeFrom = 1;
                options.SmallestEdgeTo = 240;
                options.CheckpointPath = checkpointPath;
                options.ResultsPath = resultsPath;
                options.CheckpointEveryVertices = 1;
                options.ProgressSeconds = 3600;
                options.Resume = false;

                CompleteEulerSearchResult first =
                    new CompleteEulerBrickSearch(1, 240, null, options).Search();

                options.Resume = true;
                CompleteEulerSearchResult resumed =
                    new CompleteEulerBrickSearch(1, 240, null, options).Search();

                TestCondition(
                    "Checkpointed complete search finishes its first run",
                    first.PrimitiveEulerBricks == 1 &&
                    File.ReadAllText(checkpointPath).Contains("done=1"));
                TestCondition(
                    "Completed checkpoint resumes without repeating enumeration",
                    resumed.PrimitiveEulerBricks == 0);
                TestCondition(
                    "Persistent results contain a run summary",
                    File.ReadAllText(resultsPath).Contains("summary"));
            }
            finally
            {
                if (File.Exists(checkpointPath)) File.Delete(checkpointPath);
                if (File.Exists(resultsPath)) File.Delete(resultsPath);
            }
        }

        private static void TestCondition(string name, bool condition)
        {
            _AllPassed = _AllPassed && condition;
            Console.WriteLine("{0} : Test {1}", condition ? "Succeeded" : "Failed", name);
        }

    }
}
