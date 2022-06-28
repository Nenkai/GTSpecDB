using System;
using System.Collections.Generic;
using System.Text;

using System.IO;
using Syroot.BinaryData;

namespace GTSpecDB.Core
{
    public class SpecDBDebugPrinter
    {
        private BinaryStream _stream;

        public List<EntryInfo> _entryInfos;
        public short[] HuffmanTable { get; set; } = new short[0x100];

        /* Step 1: Get DB Index from DB Row using top table which is ordered 
         * Step 2: Get entry info by index, get the second int, which is offset to short table then get the HuffManpart from the "short table"
         * 
         * */
        public void Load(string tablePath)
        {
            _stream = new BinaryStream(new FileStream(tablePath, FileMode.Open));

            _stream.Position = 0x08;
            uint rowCount = _stream.ReadUInt32();
            uint rowSize = _stream.ReadUInt32();

            _entryInfos = new List<EntryInfo>((int)rowCount);
            for (var i = 0; i < rowCount; i++)
            {
                var info = new EntryInfo();
                info.Read(_stream);
                _entryInfos.Add(info);
            }

            uint nextTableOffset = _stream.ReadUInt32();
            uint entryCount = _stream.ReadUInt32();
            HuffmanTable = _stream.ReadInt16s(0x100);
        }

        public void Print()
        {
            PrintEntryInfos();
            PrintHuffmanTable();
        }

        private void PrintEntryInfos()
        {
            using (var tw = new StreamWriter("entry_infos.txt"))
            {
                tw.WriteLine($"Row Count: {_entryInfos.Count}");
                foreach (var entryInfo in _entryInfos)
                {
                    tw.WriteLine($"- ID: {entryInfo.ID} - Offset: {entryInfo.Offset:X8}");
                }
            }
        }

        private void PrintHuffmanTable()
        {
            using (var tw = new StreamWriter("huffman_table.txt"))
            {
                foreach (var val in HuffmanTable)
                {
                    tw.WriteLine($"- : {val:X8}");
                }
            }
        }
    }

    public class EntryInfo
    {
        public uint ID { get; set; }
        public uint Offset { get; set; }

        public void Read(BinaryStream bs)
        {
            ID = bs.ReadUInt32();
            Offset = bs.ReadUInt32();
        }
    }
}
