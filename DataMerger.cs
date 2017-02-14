using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PerfectCuboid
{
    class DataMerger
    {
        public const string PathToFile = @"Working";
        public const string PatternOfFile = "Cuboid.{0}";
        public const string FilePatternPPT = PathToFile + @"\" + PatternOfFile + ".pptf";
        public const string FilePatternNPT = PathToFile + @"\" + PatternOfFile + ".nptf";

        private List<string> _files = new List<string>();
        private SortedList<DataNode, DataIOReader> _dataQueue = new SortedList<DataNode, DataIOReader>();

        public DataMerger()
        {
            // Explicitly handle FilePatternNPT only since PPT has no need to merge.
            string pattern = string.Format(Path.GetFileName(FilePatternNPT), "*");
            _files.AddRange(Directory.GetFiles(PathToFile, pattern));
        }

        /// <summary>
        /// Get next data node from merged list
        /// </summary>
        /// <returns></returns>
        public DataNode GetNextDataNode()
        {
            DataNode ret = null;
            if (_dataQueue.Count > 0)
            {
                ret = _dataQueue.ElementAt(0).Key;
                DataNode dnNext = _dataQueue.ElementAt(0).Value.ReadNode();
                //Console.WriteLine("dnNext={0}", dnNext.ToString());
                while ((dnNext != null) &&
                    _dataQueue.ContainsKey(dnNext))
                {
                    dnNext = _dataQueue.ElementAt(0).Value.ReadNode();
                }

                if (dnNext != null)
                {
                    _dataQueue.Add(dnNext, _dataQueue.ElementAt(0).Value);
                }

                _dataQueue.RemoveAt(0);
            }

            return ret;
        }

        public void InitializeQueue()
        {
            // Merge 1 with 2, then 1 & 2 with 3, then 1 & 2 & 3 with 4, ...
            // How to resue existing file, meant how to file sort in place?
            // Files named as 1 to n, and we do merge from second, since there
            // is no need to merge if only one files existing.
            if (_files.Count == 0)
            {
                return;
            }

            for (int n = 1; n <= _files.Count; n++)
            {
                string fileN = string.Format(FilePatternNPT, n);
                DataIOReader readerN = new PerfectCuboid.DataIOReader(fileN);
                DataNode dnNext = readerN.ReadNode();
                //Console.WriteLine("N={0}, dnN={1}", n, dnNext.ToString());
                while ((dnNext != null) &&
                    _dataQueue.ContainsKey(dnNext))
                {
                    dnNext = _dataQueue.ElementAt(0).Value.ReadNode();
                }

                if (dnNext != null)
                {
                    _dataQueue.Add(dnNext, readerN);
                }
            }
        }

        public string Merge()
        {
            string fileNew = string.Format(FilePatternNPT, "merged");
            if (File.Exists(fileNew))
            {
                Console.WriteLine("Already exists merged file, skip merge and continue");
                return fileNew;
            }

            InitializeQueue();

            using (FileStream stream = new FileStream(fileNew, FileMode.CreateNew))
            {
                using (BinaryWriter dataNew = new BinaryWriter(stream))
                {
                    while (_dataQueue.Count > 0)
                    {
                        GetNextDataNode().Write(dataNew);
                    }
                }
            }

            return fileNew;
        }

        /// <summary>
        /// Merge first two files to third one.
        /// </summary>
        /// <param name="fileM"></param>
        /// <param name="fileN"></param>
        /// <param name="fileNew"></param>
        private void MergeTwoFiles(string fileM, string fileN, string fileNew)
        {
            Console.WriteLine("Merge file {0}, with {1}, start at {2}", fileM, fileN, DateTime.Now);
            DataIOReader dataFileM = new DataIOReader(fileM);
            DataIOReader dataFileN = new DataIOReader(fileN);
            using (FileStream stream = new FileStream(fileNew, FileMode.CreateNew))
            {
                using (BinaryWriter dataNew = new BinaryWriter(stream))
                {
                    DataNode dnM = dataFileM.ReadNode();
                    DataNode dnN = dataFileN.ReadNode();
                    while (dnM != null && dnN != null)
                    {
                        if (dnM.CompareTo(dnN) <= 0)
                        {
                            dnM.Write(dataNew);
                            while (dnN != null && dnM.CompareTo(dnN) == 0)
                            {
                                // remove duplicate in dataFileN
                                dnN = dataFileN.ReadNode();
                            }
                            DataNode dnOld = dnM;
                            dnM = dataFileM.ReadNode();
                            while (dnM != null && dnM.CompareTo(dnOld) == 0)
                            {
                                // remove duplicate in dataFileM
                                dnM = dataFileM.ReadNode();
                            }

                        }
                        else
                        {
                            dnN.Write(dataNew);
                            dnN = dataFileN.ReadNode();
                        }
                    }

                    while (dnM != null)
                    {
                        dnM.Write(dataNew);
                        dnM = dataFileM.ReadNode();
                    }

                    while (dnN != null)
                    {
                        dnN.Write(dataNew);
                        dnN = dataFileN.ReadNode();
                    }
                }
            }
            Console.WriteLine("\tMerge file {0}, with {1}, end at {2}", fileM, fileN, DateTime.Now);

            dataFileM.Close();
            dataFileN.Close();
        }
    }
}
