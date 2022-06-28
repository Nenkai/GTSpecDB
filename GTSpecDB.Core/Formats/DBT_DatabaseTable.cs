using System;
using System.Buffers.Binary;

using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

namespace GTSpecDB.Core.Formats
{
    /// <summary>
    /// Database Table.
    /// </summary>
    public class DBT_DatabaseTable
    {
        public const int HeaderSize = 0x10;

        public Endian Endian { get; }
        public byte[] Buffer { get; }

        public DBT_DatabaseTable(byte[] buffer, Endian endian)
        {
            Buffer = buffer;
            Endian = endian;
        }

        public int HuffmanTableHeaderOffset { get; set; }
        public int HuffmanTableEntryOffset { get; set; }

        public int SearchTableOffset { get; set; }

        public int DataMapHeaderOffset { get; set; }
        public int DataMapOffset { get; set; }

        public int ShortTableOffset { get; set; }

        public int EntryCount
        {
            get
            {
                if (Endian == Endian.Little)
                    return BinaryPrimitives.ReadInt32LittleEndian(Buffer.AsSpan(0x08));
                else
                    return BinaryPrimitives.ReadInt32BigEndian(Buffer.AsSpan(0x08));
            }
        }

        public int RowDataLength
        {
            get
            {
                if (Endian == Endian.Little)
                    return BinaryPrimitives.ReadInt32LittleEndian(Buffer.AsSpan(0x0C));
                else
                    return BinaryPrimitives.ReadInt32BigEndian(Buffer.AsSpan(0x0C));
            }
        }

        public int VersionHigh
        {
            get
            {
                if (Endian == Endian.Little)
                    return BinaryPrimitives.ReadInt16LittleEndian(Buffer.AsSpan(0x04));
                else
                    return BinaryPrimitives.ReadInt16BigEndian(Buffer.AsSpan(0x04));
            }
        }

        public int GetIndexOfID(int targetRowId)
        {
            SpanReader sr = new SpanReader(Buffer, Endian);
            int entryCount = EntryCount;

            int max = entryCount - 1;
            int min = -1;
            if (entryCount > 0)
            {
                while (true)
                {
                    int mid = max / 2;

                    sr.Position = HeaderSize + (mid * 8);
                    int currentRowId = sr.ReadInt32();

                    if (currentRowId == targetRowId)
                        return mid;

                    if (targetRowId <= currentRowId)
                    {
                        entryCount = mid;
                        mid = min;
                    }

                    if (entryCount <= mid + 1)
                        break;

                    max = mid + entryCount;
                    min = mid;
                }
            }

            return -1;
        }

        public Span<byte> ExtractRow(ref SpanReader sr)
        {
            ExtractHuffmanPart(ref sr, out Span<byte> entryDataBuffer);
            return ExtractDiffDictPart(entryDataBuffer);
        }


        Span<byte> ExtractDiffDictPart(Span<byte> entryData)
        {
            Span<byte> rawEntryData = entryData.Slice(1);
            byte type = (byte)(entryData[0] >> 6);
            int rowLength = RowDataLength;
            int dataIndex = entryData[0] & 0b11_1111;

            if (type == 0) // Copy row from shared full row data
            {
                var sr = new SpanReader(Buffer, Endian);
                sr.Position = DataMapOffset + (dataIndex * rowLength);
                return sr.ReadBytes(rowLength);
            }
            else if (type == 1) // Row from raw entry data
            {
                return rawEntryData.Slice(0, rowLength);
            }
            else if (type == 2) // Differences
            {
                var sr = new SpanReader(Buffer, Endian);
                sr.Position = DataMapOffset + (dataIndex * rowLength);
                Span<byte> rowData = sr.ReadBytes(rowLength);

                var entryDataOffset = 1 + (rowLength / 8);
                if ((rowLength % 8) != 0)
                    entryDataOffset++;

                for (var i = 0; i < rowLength; i++)
                {
                    var val = rawEntryData.Slice(i / 8)[0] >> (i % 8);
                    if ((val & 0x01) != 0 && entryDataOffset < entryData.Length)
                        rowData[i] = entryData[entryDataOffset++];
                }


                return rowData;
            }

            throw new Exception($"ExtractDiffDictPart Errored: got unsupported type {type}");
        }


        public int ExtractHuffmanPart(ref SpanReader sr, out Span<byte> huffmanDict)
        {
            byte huffmanPartCount = sr.Span[sr.Position];
            huffmanDict = new byte[huffmanPartCount];
            int basePos = sr.Position;
            
            int bitOffset = 0;
            for (int i = 0; i < huffmanPartCount; i++)
            {

                int currentByte = bitOffset / 8;
                sr.Position = basePos + currentByte + 1;

                // Bury this and never look at it. Mystic PD shit.
                uint val = 0;
                val += !sr.IsEndOfSpan ? sr.ReadByte() : 0u;
                val += !sr.IsEndOfSpan ? ((uint)sr.ReadByte() << 8) : 0u;
                val += !sr.IsEndOfSpan ? ((uint)sr.ReadByte() << 16) : 0u;
                val += !sr.IsEndOfSpan ? ((uint)sr.ReadByte() << 24) : 0u;
                val >>= bitOffset - (currentByte * 8);

                Span<byte> b = huffmanDict.Slice(i);
                bitOffset += (int)ProcessHuffmanCode(val, ref b);
            }
            
            sr.Position = basePos;
            return huffmanPartCount;
        }

        public uint ProcessHuffmanCode(uint val, ref Span<byte> huffmanPart)
        {
            SpanReader sr = new SpanReader(Buffer, Endian);
            sr.Position = (int)(HuffmanTableEntryOffset + (byte)val * 2);

            // Read the byte after the current pos
            uint next = sr.Span[sr.Position + 1];
            if (next == 0)
            {
                // Found it?
                next = SearchHuffmanCode(val, ref huffmanPart);
            }
            else
                // Not yet
                huffmanPart[0] = sr.Span[sr.Position];

            return next;
        }

        public uint SearchHuffmanCode(uint key, ref Span<byte> outEntryData)
        {
            for (uint bitIndex = 9; bitIndex < 32; bitIndex++) 
            {
                uint targetIndex = (uint)(key & (1 << (int)bitIndex) - 1);
                int entryCount = ReadInt32(Buffer.AsSpan(HuffmanTableHeaderOffset + 4), Endian);

                int max = entryCount;
                int min = -1;
                int mid;
                do
                {
                    mid = (max + min) / 2;
                    Span<byte> searchEntry = Buffer.AsSpan(SearchTableOffset + mid * 8);
                    byte bitLocation = searchEntry[0];
                    int searchIndex = ReadInt32(searchEntry.Slice(4), Endian);
                    if (searchIndex == targetIndex && bitLocation == bitIndex)
                    {
                        outEntryData[0] = searchEntry[1]; // Key
                        return bitIndex;
                    }

                    if (bitIndex > bitLocation)
                        min = mid;
                    else if (bitIndex < bitLocation)
                        max = mid;
                    else if (bitLocation == bitIndex)
                    {
                        if (targetIndex > searchIndex)
                            min = mid;
                        else
                            max = mid;
                    }
                } while (min + 1 != max);
            }

            return 0;
        }

        public int ReadInt32(Span<byte> buffer, Endian endian)
        {
            return endian == Endian.Big ?
                      BinaryPrimitives.ReadInt32BigEndian(buffer)
                    : BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
    }
}
