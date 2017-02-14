using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PerfectCuboid
{
    struct ThreadParameters
    {
        public int ThreadID;
        public UInt64 Low;
        public UInt64 High;
        public DataList dl;
    }

    class DataNode : IComparable
    {
        public BigInteger _A = 0;
        public BigInteger _B = 0;
        public BigInteger _C = 0;
        public BigInteger _G = 0;

        public DataNode(BigInteger A, BigInteger B, BigInteger G)
        {
            // this is a Pythogrean Triangle
            _A = A;
            _B = B;

            Utils.InOrder(ref _A, ref _B);

            _G = G;
        }

        public DataNode(BigInteger A, BigInteger B, BigInteger C, BigInteger G)
        {
            _A = A;
            _B = B;
            _C = C;

            Utils.InOrder(ref _A, ref _B);
            Utils.InOrder(ref _A, ref _C);
            Utils.InOrder(ref _B, ref _C);

            _G = G;
        }

        public int CompareTo(object o)
        {
            DataNode other = (DataNode)o;

            if (this._G != other._G)
            {
                return (this._G > other._G) ? 1 : -1;
            }

            if (this._A != other._A)
            {
                return (this._A > other._A) ? 1 : -1;
            }

            if (this._B != other._B)
            {
                return (this._B > other._B) ? 1 : -1;
            }

            if (this._C != other._C)
            {
                return (this._C > other._C) ? 1 : -1;
            }

            return 0;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write((UInt64)_A);
            bw.Write((UInt64)_B);
            if (_C > 0)
            {
                bw.Write((UInt64)_C);
            }
            bw.Write((UInt64)_G);
        }

        public int Write(byte[] array, int startIndex)
        {
            int nextIndex = Write(array, startIndex, (UInt64)_A);
            nextIndex = Write(array, nextIndex, (UInt64)_B);
            if (_C > 0)
            {
                nextIndex = Write(array, nextIndex, (UInt64)_C);
            }
            nextIndex = Write(array, nextIndex, (UInt64)_G);

            return nextIndex;
        }
        private int Write(byte[] array, int startIndex, UInt64 data)
        {
            byte[] converted = BitConverter.GetBytes(data);
            foreach (byte b in converted)
            {
                array[startIndex++] = b;
            }

            return startIndex;
        }



        public void Read(BinaryReader br)
        {
            // Only read PT now.
            _A = (UInt64)br.ReadInt64();
            _B = (UInt64)br.ReadInt64();
            _G = (UInt64)br.ReadInt64();
        }

        public new string ToString()
        {
            if (_C > 0)
            {
                return string.Format("{0}^2 + {1}^2 + {2}^2 = {3}^2", _A, _B, _C, _G);
            }
            else
            {
                return string.Format("{0}^2 + {1}^2 = {2}^2", _A, _B, _G);
            }
        }
    }


    struct DataSet
    {
        public bool valid;
        public UInt64 m;
        public UInt64 n;
        public DataNode pt; // PythogreanTriangle
        public DataNode eb; // EulerBrick
        //public DataNode eb2; // EulerBrick 2
        public BigInteger check;
        public BigInteger check2;

        public DataSet(UInt64 mIn, UInt64 nIn)
        {
            valid = false;

            m = mIn;
            n = nIn;

            BigInteger m2 = m * m;
            BigInteger n2 = n * n;
            BigInteger a = m2 - n2;
            BigInteger b = (m * n) << 1;
            BigInteger c = m2 + n2;
            pt = new DataNode(a, b, c);

            // we have PT now: m2_minus_n2, mn2, m2_plus_n2
            // Generate Euler Brick now

            // generate Euler Brick
            // X = a * (4b^2 - c^2), Y = b * (4a^2 - c^2), Z = 4abc
            // X^2 + Y^2 = C^6, X^2 + Y^2 = a(5b^2+a^2), X^2 + Z^2 = b(5a^2+b^2)
            BigInteger b_diff_c = Utils.BigDataAbsoluteSquareDiff((b << 1), c);
            BigInteger a_diff_c = Utils.BigDataAbsoluteSquareDiff((a << 1), c);
            BigInteger X = a * b_diff_c;
            BigInteger Y = b * a_diff_c;
            BigInteger Z = (a << 2) * b * c;
            BigInteger G = X * X + Y * Y + Z * Z;
            eb = new DataNode(X, Y, Z, G);
            //eb2 = new DataNode(X*Y, Y*Z, Z*X, ...)
            
            // X^2 + Y^2 + Z^2 = c^6 + (4abc)^2 = c^2(16a^2b^2 + c^4). 
            // Thus, check if G is a perfect square is the same as check "16a^2b^2+c^4" is a perfect square.
            // To save the calculation of c, (16a^2b^2 + c^4) or = (16a^2b^2 + a^4 + 2a^2b^2 + b^4) = (a^4 + 18a^2b^2 + b^4)
            BigInteger a2 = a * a;
            BigInteger b2 = b * b;
            //BigInteger a2b2_18 = a2 * b2 * 18;
            BigInteger a2b2_16 = (a2 * b2) << 4;
            check = a2b2_16 + c * c * c * c;
            BigInteger xyNoAB = b_diff_c * a_diff_c;
            BigInteger yzNoAB = Y * (c << 2);
            BigInteger zxNoAB = (Z << 2) * X;
            //check2 = xy * xy + yz * yz + zx * zx;
            // Since xy, yz, zx are all contains ab, thus the final check can save a^2b^2

            // check2 = BigInteger.One;
            check2 = xyNoAB * xyNoAB + yzNoAB * yzNoAB + zxNoAB * zxNoAB;
        }

        public bool DataCheck()
        {
            UInt64 lastD = (UInt64)(check % 10);
            if ((lastD == 1) || (lastD == 5) || (lastD == 9))
            {
                valid = true;
                if (Utils.IsPerfectSquare(check))
                {
                    return true;
                }
            }

            lastD = (UInt64)(check2 % 10);
            if ((lastD == 1) || (lastD == 5) || (lastD == 9))
            {
                valid = true;
                if (Utils.IsPerfectSquare(check2))
                {
                    return true;
                }
            }

            return false;
        }

        public override string ToString()
        {
            //return string.Format("{0},{1} ==> ({2})^2+({3})^2=({4})^2 ==> ({5})^2+({6})^2+({7})^2=({8}), check:{9}",
            //    m, n, a, b, c, X, Y, Z, G, check);
            //return string.Format("==>{0},{1}==>{2}^2+{3})^2=({4})^2 check:{5}",
            //    m, n, pt._A, pt._B, pt._C, check);
            return string.Format("==>{0},{1}==> check:{2}, check2:{3}",
                m, n, check, check2);
        }
    }

    struct DataSet1
    {
        public bool valid;
        public BigInteger m;
        public BigInteger n;
        public BigInteger n8;
        public BigInteger sums;
        public BigInteger check;

        public DataSet1(UInt64 mIn, UInt64 nIn)
        {
            valid = false;
            m = mIn;
            n = nIn;
            BigInteger m2 = m * m;
            BigInteger m4 = m2 * m2;
            BigInteger m6 = m4 * m2;
            BigInteger m8 = m4 * m4;
            BigInteger n2 = n * n;
            BigInteger n4 = n2 * n2;
            BigInteger n6 = n4 * n2;
            n8 = n4 * n4;
            sums = m8 + 68 * m6 * n2 - 122 * m4 * n4 + 68 * m2 * n6;
            check = sums + n8;
        }

        public bool DataCheck()
        {
            BigInteger result = sums / n8;
            if (sums == result * n8)
            {
                valid = true;
                return Utils.IsPerfectSquare(check);
            }
            return false;
        }

        public override string ToString()
        {
            //return string.Format("{0},{1} ==> ({2})^2+({3})^2=({4})^2 ==> ({5})^2+({6})^2+({7})^2=({8}), check:{9}",
            //    m, n, a, b, c, X, Y, Z, G, check);
            return string.Format("==>{0},{1} ==> sums:{2}, n8:{3}",
                m, n, sums, n8);
        }
    }


    //public class BigUInt_Deprecated
    //{
    //    public int Length = 0;

    //    protected List<UInt64> _data = new List<ulong>();

    //    private UInt64 _lastDigit = 0xFFFFFFFFFFFFFFFF;

    //    public BigUInt(params UInt64[] data)
    //    {
    //        _data.AddRange(data);
    //        CountLength();
    //    }

    //    public BigUInt(params BigUInt[] data)
    //    {
    //        for (int i = 0; i < data.Length; i++)
    //        {
    //            _data.AddRange(data[i]._data);
    //        }
    //        CountLength();
    //    }

    //    public BigUInt(List<UInt64> data)
    //    {
    //        _data.AddRange(data);
    //        CountLength();
    //    }

    //    private void CountLength()
    //    {
    //        int count = 0;
    //        for (int i = 0; i < _data.Count; i++)
    //        {
    //            if (_data[i] == 0)
    //            {
    //                count++;
    //            }
    //            else
    //            {
    //                break;
    //            }
    //        }

    //        if (count > 0)
    //        {
    //            _data.RemoveRange(0, count);
    //        }

    //        Length = _data.Count;
    //    }

    //    public bool IsOdd()
    //    {
    //        return ((_data[Length - 1] & 0x1) == 1);
    //    }

    //    public UInt64 LastDigit
    //    {
    //        get
    //        {
    //            if (_lastDigit > 10)
    //            {
    //                // Not calculated yet
    //                _lastDigit = _data[0] % 10;
    //                for (int i = 1; i < Length; i++)
    //                {
    //                    // 2^64 end of 6
    //                    _lastDigit = (_lastDigit * 6 + _data[i]) % 10;
    //                }
    //            }

    //            return _lastDigit;
    //        }
    //    }

    //    public bool IsPerfectSquare()
    //    {
    //        UInt64[] bitData = new UInt64[Length];
    //        bitData[0] = 0x4000000000000000;
    //        BigUInt bit = new PerfectCuboid.BigUInt(bitData);
    //        BigUInt num = new PerfectCuboid.BigUInt(_data);
    //        BigUInt res = new BigUInt(0);

    //        // "bit" starts at the highest power of four <= the argument.
    //        while (bit > num)
    //        {
    //            bit >>= 2;
    //        }

    //        while (bit.Length > 0)
    //        {
    //            if (num < res + bit)
    //            {
    //                res >>= 1;
    //            }
    //            else
    //            {
    //                num -= res + bit;
    //                res = (res >> 1) + bit;
    //            }
    //            bit >>= 2;
    //        }

    //        return num.Length == 0;
    //    }

    //    public BigUInt Square()
    //    {
    //        return this * this;
    //    }

    //    // To make it simple, all the operators supposed to have the same length
    //    public static BigUInt operator +(BigUInt a1, BigUInt a2)
    //    {
    //        List<UInt64> sum = new List<ulong>();
    //        int a1L = a1.Length;
    //        int a2L = a2.Length;

    //        UInt64 carr = 0;
    //        UInt64 nextCarr = 0;
    //        UInt64 aData = 0;
    //        UInt64 bData = 0;
    //        while (a1L > 0 || a2L > 0)
    //        {
    //            --a1L;
    //            --a2L;
    //            aData = a1L >= 0 ? a1._data[a1L] : 0;
    //            bData = a2L >= 0 ? a2._data[a2L] : 0;
    //            nextCarr = GetCarrier(aData, bData, carr);
    //            sum.Insert(0, aData + bData + carr);
    //            carr = nextCarr;
    //        }

    //        if (carr > 0)
    //        {
    //            sum.Insert(0, carr);
    //        }

    //        return new BigUInt(sum);
    //    }

    //    public static BigUInt operator -(BigUInt a1, BigUInt a2)
    //    {
    //        List<UInt64> diff = new List<ulong>();
    //        UInt64 carr = 0;
    //        UInt64 nextCarr = 0;
    //        int a1L = a1.Length;
    //        int a2L = a2.Length;
    //        UInt64 aData = 0;
    //        UInt64 bData = 0;
    //        while (a1L > 0 || a2L > 0)
    //        {
    //            --a1L;
    //            --a2L;
    //            aData = a1L >= 0 ? a1._data[a1L] : 0;
    //            bData = a2L >= 0 ? a2._data[a2L] : 0;
    //            nextCarr = (UInt64)(aData < bData || aData-carr < bData ? 1 : 0);
    //            diff.Insert(0, aData - bData - carr);
    //            carr = nextCarr;
    //        }

    //        // ignore if there is nextCarr > 0, as the int implemented like that way

    //        return new BigUInt(diff);
    //    }

    //    public static BigUInt operator *(BigUInt x, BigUInt y)
    //    {
    //        // suppose x and y has the same length, and return will be doubled length
    //        UInt64[] products = new UInt64[x.Length + y.Length];

    //        UInt64 carr = 0;
    //        UInt64 nextCarr = 0;
    //        for (int i = y.Length-1; i >= 0; i--)
    //        {
    //            int index = products.Length - y.Length + i;
    //            for (int j = x.Length-1; j >= 0; j--)
    //            {
    //                int indexPos = index - x.Length + j + 1;
    //                int pos = -1;
    //                BigUInt product = Multiply(x._data[j], y._data[i]);
    //                for (int k = product.Length - 1; k >= 0; k--)
    //                {
    //                    pos = indexPos - product.Length + k + 1;
    //                    nextCarr = GetCarrier(products[pos], product._data[k], carr);
    //                    products[pos] += product._data[k];
    //                    products[pos] += carr;
    //                    carr = nextCarr;
    //                }
    //                if (carr > 0)
    //                {
    //                    pos--;
    //                    products[pos] += carr;
    //                    carr = 0;
    //                }
    //            }
    //        }

    //        return new BigUInt(products);
    //    }

    //    public static bool operator ==(BigUInt a1, BigUInt a2)
    //    {
    //        if (a1.Length != a2.Length)
    //        {
    //            return false;
    //        }

    //        for (int i = a1.Length -1; i >= 0; i--)
    //        {
    //            if (a1._data[i] != a2._data[i])
    //            {
    //                return false;
    //            }
    //        }

    //        return true;
    //    }

    //    public static bool operator != (BigUInt a1, BigUInt a2)
    //    {
    //        return !(a1 == a2);
    //    }

    //    public static bool operator < (BigUInt a1, BigUInt a2)
    //    {
    //        if (a1.Length < a2.Length)
    //        {
    //            return true;
    //        }
    //        else if (a1.Length > a2.Length)
    //        {
    //            return false;
    //        }
    //        for (int i = 0; i < a1.Length; i++)
    //        {
    //            if (a1._data[i] < a2._data[i])
    //            {
    //                return true;
    //            }
    //            else if (a1._data[i] > a2._data[i])
    //            {
    //                return false;
    //            }
    //        }
    //        return false;
    //    }

    //    public static bool operator <=(BigUInt a1, BigUInt a2)
    //    {
    //        if (a1.Length < a2.Length)
    //        {
    //            return true;
    //        }
    //        else if (a1.Length > a2.Length)
    //        {
    //            return false;
    //        }
    //        for (int i = 0; i < a1.Length; i++)
    //        {
    //            if (a1._data[i] < a2._data[i])
    //            {
    //                return true;
    //            }
    //            else if (a1._data[i] > a2._data[i])
    //            {
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //    public static bool operator > (BigUInt a1, BigUInt a2)
    //    {
    //        if (a1.Length > a2.Length)
    //        {
    //            return true;
    //        }
    //        else if (a1.Length < a2.Length)
    //        {
    //            return false;
    //        }
    //        for (int i = 0; i < a1.Length; i++)
    //        {
    //            if (a1._data[i] > a2._data[i])
    //            {
    //                return true;
    //            }
    //            else if (a1._data[i] < a2._data[i])
    //            {
    //                return false;
    //            }
    //        }
    //        return false;
    //    }

    //    public static bool operator >=(BigUInt a1, BigUInt a2)
    //    {
    //        if (a1.Length > a2.Length)
    //        {
    //            return true;
    //        }
    //        else if (a1.Length < a2.Length)
    //        {
    //            return false;
    //        }
    //        for (int i = 0; i < a1.Length; i++)
    //        {
    //            if (a1._data[i] > a2._data[i])
    //            {
    //                return true;
    //            }
    //            else if (a1._data[i] < a2._data[i])
    //            {
    //                return false;
    //            }
    //        }
    //        return true;
    //    }

    //    public static BigUInt operator >>(BigUInt a, int n)
    //    {
    //        // Note: only handle n < 64 case
    //        int nLeft = 64 - n;
    //        UInt64 shift = 0;
    //        UInt64 nextShift = 0;
    //        for (int i = 0; i < a.Length; i++)
    //        {
    //            nextShift = a._data[i] << nLeft;
    //            a._data[i] >>= n;
    //            a._data[i] |= shift;
    //            shift = nextShift;
    //        }
    //        a.CountLength();
    //        return a;
    //    }

    //    public static BigUInt operator <<(BigUInt a, int n)
    //    {
    //        // Note: only handle n < 64 case
    //        int nRight = 64 - n;
    //        UInt64 shift = 0;
    //        UInt64 nextShift = 0;
    //        for (int i = a.Length-1; i >= 0; i--)
    //        {
    //            nextShift = a._data[i] >> nRight;
    //            a._data[i] <<= n;
    //            a._data[i] |= shift;
    //            shift = nextShift;
    //        }
    //        if (shift > 0)
    //        {
    //            a._data.Insert(0, shift);
    //        }
    //        a.CountLength();
    //        return a;
    //    }

    //    // avoid division

    //    public override bool Equals(object obj)
    //    {
    //        return base.Equals(obj);
    //    }

    //    public override int GetHashCode()
    //    {
    //        return base.GetHashCode();
    //    }
    //    public override string ToString()
    //    {
    //        StringBuilder sb = new StringBuilder();
    //        if (Length == 0)
    //        {
    //            return "0";
    //        }
    //        for (int i = 0; i < Length-1; i++)
    //        {
    //            sb.Append(_data[i]);
    //            sb.Append("-");
    //        }
    //        sb.Append(_data[Length - 1]);
    //        return sb.ToString();
    //    }

    //    private static UInt64 GetCarrier(UInt64 a, UInt64 b, UInt64 c)
    //    {
    //        UInt64 ret = 0;
    //        if (UInt64.MaxValue - a < b)
    //        {
    //            ret++;
    //            if ((UInt64.MaxValue - a) + (UInt64.MaxValue - b) < c)
    //            {
    //                ret++;
    //            }
    //        }
    //        else if (UInt64.MaxValue - a -b < c)
    //        {
    //            ret++;
    //        }

    //        return ret;
    //    }

    //    private static BigUInt Multiply(UInt64 X, UInt64 Y)
    //    {
    //        List<UInt64> product = new List<UInt64>(2);

    //        // a is high 32 bits and b is low 32 bits
    //        UInt32 Xh = (UInt32)(X >> 32);
    //        UInt32 Xl = (UInt32)(X);

    //        UInt32 Yh = (UInt32)(Y >> 32);
    //        UInt32 Yl = (UInt32)(Y);

    //        UInt64 Xh_Yh = (UInt64)Xh * (UInt64)Yh;
    //        UInt64 Xl_Yl = (UInt64)Xl * (UInt64)Yl;
    //        UInt64 Xh_Yl = (UInt64)Xh * (UInt64)Yl;
    //        UInt64 Xl_Yh = (UInt64)Xl * (UInt64)Yh;

    //        // X * Y = (Xh + Xl)*(Yh + Yl) = Xh*Yh + Xh*Yl + Xl*Yh + Xl*Yl
    //        // Note, Xh * Yl and Xl * Yh has half in high and half in low parts
    //        product.Add(Xh_Yh + (Xh_Yl >> 32) + (Xl_Yh >> 32));

    //        // check the low end if there is a carrier or not.
    //        product[0] += GetCarrier(Xl_Yl, (Xh_Yl << 32), (Xl_Yh << 32));
    //        product.Add(Xl_Yl + (Xh_Yl << 32) + (Xl_Yh << 32));

    //        return new BigUInt(product);
    //    }
    //}

    class Utils
    {
        public static bool IsPerfectSquare(BigInteger N)
        {
            //if (N > 0) return false;
            int shift = (((int)(BigInteger.Log(N, 2)) + 1) >> 1) << 1;
            BigInteger bit = BigInteger.One << shift;
            BigInteger num = N;
            BigInteger res = 0;

            // "bit" starts at the highest power of four <= the argument.
            while (bit > num)
            {
                bit >>= 2;
            }

            while (bit > 0)
            {
                if (num < res + bit)
                {
                    res >>= 1;
                }
                else
                {
                    num -= res + bit;
                    res = (res >> 1) + bit;
                }
                bit >>= 2;
            }

            return num == 0;
        }
        public static BigInteger BigDataAbsoluteSquareDiff(BigInteger a, BigInteger b)
        {
            // return |a2 - b2|
            return a > b ? a*a - b*b : b*b - a*a;
        }

        public static bool InOrder(ref BigInteger a, ref BigInteger b)
        {
            bool swapped = false;
            // end by a < b
            if (a > b)
            {
                BigInteger temp = b;
                b = a;
                a = temp;
                swapped = true;
            }

            return swapped;
        }

        public static void Output(System.IO.TextWriter tw, string output)
        {
            Console.WriteLine(output);
            if (tw != null)
            {
                tw.WriteLine(output);
                tw.Flush();
            }
        }
    }
}
