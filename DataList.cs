using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;

namespace PerfectCuboid
{
    class DataList
    {
        public static object _lock = new object();

        // Only public static SortedSet can be thread safe
        public SortedSet<DataNode> _nodes = new SortedSet<DataNode>();

        public static int _totalThreads = 0;
        public static int _readyToWriteCount = 0;
        public static UInt64 _totalCount = 0;

        private static int _fileIndex = 0;
        public static int _countInFile = 0;

        private const int Size_Of_DataNode = 24;
        private const int Max_Binary_Data_Length = Size_Of_DataNode << 20; // x M
        private static byte[] _writeBuffer = new byte[Max_Binary_Data_Length + Size_Of_DataNode];
        private static int _writeIndex = 0;
         
        public DataList()
        {
        }

        public static void InitStaticData()
        {
            NameValueCollection appSettings = ConfigurationManager.AppSettings;
            _countInFile = int.Parse(appSettings["ColuntInFile"]);
            _totalThreads = int.Parse(appSettings["Threads"]);
        }

        public void Add(UInt64 key, UInt64 pair, UInt64 value)
        {
            Add(new DataNode(key, pair, value));
        }

        public void Add(DataNode dn)
        {
            if (_readyToWriteCount > _countInFile)
            {
                lock (_lock) ;
            }
            _nodes.Add(dn);
            // Some nodes will be filtered out in _nodes for duplicates. 
            // To make it simple, no need to check, still increment the _readyToWriteCount,
            // but need to recaculate the exactly count based on the sum of all DataList instances.
            System.Threading.Interlocked.Increment(ref DataList._readyToWriteCount);
        }

        public static void Output(DataList[] dataLists, TextWriter twOutput)
        {
            // using count as temp to keep _readyToWriteCount not changed before write completed
            int count = 0;

            SortedSet<DataNode>.Enumerator[] enumerators = new SortedSet<DataNode>.Enumerator[dataLists.Length];
            SortedList<DataNode, SortedSet<DataNode>.Enumerator> dataQueue = new SortedList<DataNode, SortedSet<DataNode>.Enumerator>();
            foreach (DataList dl in dataLists)
            {
                count = count + dl._nodes.Count;
                SortedSet<DataNode>.Enumerator dlEnum = dl._nodes.GetEnumerator();
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
            Utils.Output(twOutput, string.Format(".FileIndex={0}, time={1}, totalCount={2}", _fileIndex, DateTime.Now, _totalCount));

            using (FileStream stream = new FileStream(filename, FileMode.CreateNew))
            {
                using (BinaryWriter tw = new BinaryWriter(stream))
                {
                    _writeIndex = 0;
                    int skip = 0;
                    DataNode dn, dnNext;
                    SortedSet<DataNode>.Enumerator dnEnum;
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

            foreach (DataList dl in dataLists)
            {
                dl._nodes.Clear();
            }

            // Write completed, reset _readyToWriteCount to 0
            System.Threading.Interlocked.Exchange(ref DataList._readyToWriteCount, 0);
        }
    }

    class DataIOReader
    {
        private BinaryReader _reader;
        private FileStream _stream;
        private bool _disposed = true;
        private string _filename = null;

        public DataIOReader(string filename)
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

        ~DataIOReader()
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

        public DataNode ReadNode()
        {
            try
            {
                DataNode d = new DataNode(0,0,0);
                d.Read(_reader);
                return d;
            }
            catch (Exception)
            { }

            return null;
        }
    }
}
