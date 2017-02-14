using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace PerfectCuboid
{
    class DataNode3 : IComparable
    {
        public UInt64 A = 0;
        public UInt64 B = 0;
        public UInt64 C = 0;
        public UInt64 G = 0;

        public DataNode3(UInt64 a, UInt64 b, UInt64 c, UInt64 g)
        {
            Utils.ascend(ref a, ref b);
            Utils.ascend(ref a, ref c);
            Utils.ascend(ref b, ref c);

            A = a;
            B = b;
            C = c;
            G = g;
        }

        public int CompareTo(object o)
        {
            DataNode3 other = (DataNode3)o;

            if (this.G > 0 && other.G > 0 && this.G != other.G)
            {
                return (this.G > other.G) ? 1 : -1;
            }

            if (this.A != other.A)
            {
                return (this.A > other.A) ? 1 : -1;
            }

            if (this.B != other.B)
            {
                return (this.B > other.B) ? 1 : -1;
            }

            if (this.C != other.C)
            {
                return (this.C > other.C) ? 1 : -1;
            }


            return 0;
        }

        public void Write(BinaryWriter bw)
        {
            bw.Write(A);
            bw.Write(B);
            bw.Write(C);
            bw.Write(G);
        }

        public int Write(byte[] array, int startIndex)
        {
            int nextIndex = Write(array, startIndex, A);
            nextIndex = Write(array, nextIndex, B);
            nextIndex = Write(array, nextIndex, C);
            nextIndex = Write(array, nextIndex, G);

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
            A = (UInt64)br.ReadInt64();
            B = (UInt64)br.ReadInt64();
            C = (UInt64)br.ReadInt64();
            G = (UInt64)br.ReadInt64();
        }

        public new string ToString()
        {
            return string.Format("{0}^2 + {1}^2 + {2}^2 = {3}^2", A, B, C, G);
        }
    }

    class DataList3
    {
        public static object _lock = new object();

        // Only public static SortedSet can be thread safe
        public SortedSet<DataNode3> _nodes = new SortedSet<DataNode3>();

        public static int _totalThreads = 0;
        public static int _readyToWriteCount = 0;
        public static UInt64 _totalCount = 0;

        private static int _fileIndex = 0;
        public static int _countInFile = 0;

        private const int Size_Of_DataNode = 24;
        private const int Max_Binary_Data_Length = Size_Of_DataNode << 20; // x M
        private static byte[] _writeBuffer = new byte[Max_Binary_Data_Length + Size_Of_DataNode];
        private static int _writeIndex = 0;

        public DataList3()
        {
        }

        public static void InitStaticData()
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            _countInFile = int.Parse(appSettings["ColuntInFile"]);
            _totalThreads = int.Parse(appSettings["Threads"]);
        }

        public void Add(UInt64 A, UInt64 B, UInt64 C, UInt64 G)
        {
            Add(new DataNode3(A, B, C, G));
        }

        public void Add(DataNode3 dn)
        {
            if (_readyToWriteCount > _countInFile)
            {
                lock (_lock)
                {
                    // do nothing, just lock to syncnize the output
                };
            }
            _nodes.Add(dn);
            // Some nodes will be filtered out in _nodes for duplicates. 
            // To make it simple, no need to check, still increment the _readyToWriteCount,
            // but need to recaculate the exactly count based on the sum of all DataList instances.
            System.Threading.Interlocked.Increment(ref DataList._readyToWriteCount);
        }

        public static void Output(DataList3[] dataLists)
        {
            // using count as temp to keep _readyToWriteCount not changed before write completed
            int count = 0;

            SortedSet<DataNode3>.Enumerator[] enumerators = new SortedSet<DataNode3>.Enumerator[dataLists.Length];
            SortedList<DataNode3, SortedSet<DataNode3>.Enumerator> dataQueue = new SortedList<DataNode3, SortedSet<DataNode3>.Enumerator>();
            foreach (DataList3 dl in dataLists)
            {
                count = count + dl._nodes.Count;
                SortedSet<DataNode3>.Enumerator dlEnum = dl._nodes.GetEnumerator();
                while (dl._nodes.Count > 0 && dlEnum.MoveNext() && dlEnum.Current != null)
                {
                    if (!dataQueue.ContainsKey(dlEnum.Current))
                    {
                        dataQueue.Add(dlEnum.Current, dlEnum);
                        break;
                    }
                    else
                    {
                        // skip one, and move to next
                        count--;
                    }
                }
            }

            string filename = string.Format(DataMerger.FilePatternNPT, ++_fileIndex);
            _totalCount += (UInt64)count;
            Console.WriteLine(".FileIndex={0}, time={1}, totalCount={2}", _fileIndex, DateTime.Now, _totalCount);

            using (FileStream stream = new FileStream(filename, FileMode.CreateNew))
            {
                using (BinaryWriter tw = new BinaryWriter(stream))
                {
                    _writeIndex = 0;
                    int skip = 0;
                    DataNode3 dn, dnNext;
                    SortedSet<DataNode3>.Enumerator dnEnum;
                    while (count > 0)
                    {
                        dn = dataQueue.Keys[0];
                        dnEnum = dataQueue.Values[0];
                        dataQueue.RemoveAt(0);
                        count--;
                        if (dnEnum.MoveNext() && dnEnum.Current != null)
                        {
                            dnNext = dnEnum.Current;
                            while (dnNext != null && dataQueue.ContainsKey(dnNext))
                            {
                                // skip duplicate in case
                                dnEnum.MoveNext();
                                dnNext = dnEnum.Current;
                                count--;
                                skip++;
                            }
                        }
                        else
                        {
                            dnNext = null;
                        }

                        if (dnNext != null)
                        {
                            dataQueue.Add(dnEnum.Current, dnEnum);
                        }

                        _writeIndex = dn.Write(_writeBuffer, _writeIndex);
                        if (_writeIndex >= Max_Binary_Data_Length)
                        {
                            tw.Write(_writeBuffer, 0, _writeIndex);
                            _writeIndex = 0;
                        }
                    }

                    if (_writeIndex > 0)
                    {
                        tw.Write(_writeBuffer, 0, _writeIndex);
                        _writeIndex = 0;
                    }
                }
            }

            foreach (DataList3 dl in dataLists)
            {
                dl._nodes.Clear();
            }

            // Write completed, reset _readyToWriteCount to 0
            System.Threading.Interlocked.Exchange(ref DataList._readyToWriteCount, 0);
        }
    }

    class DataIOReader3
    {
        private BinaryReader _reader;
        private FileStream _stream;
        private bool _disposed = true;
        private string _filename = null;

        public DataIOReader3(string filename)
        {
            _filename = filename;
            Reset();
        }


        public void Reset()
        {
            Close();
            _stream = File.Open(
                _filename,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read);
            _reader = new BinaryReader(_stream);
            _disposed = false;
        }

        ~DataIOReader3()
        {
            Close();
        }

        public void Close()
        {
            if (!_disposed)
            {
                _reader.Dispose();
                _stream.Dispose();
                _disposed = true;
            }
        }

        public DataNode3 ReadNode()
        {
            try
            {
                DataNode3 d = new DataNode3(0, 0, 0, 0);
                d.Read(_reader);
                return d;
            }
            catch (Exception)
            { }

            return null;
        }
    }
}
