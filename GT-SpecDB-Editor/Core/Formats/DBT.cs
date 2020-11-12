using System;
using System.Buffers.Binary;

using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

namespace GT_SpecDB_Editor.Core.Formats
{
    public class DBT
    {
        public const int HeaderSize = 0x10;

        public Endian Endian { get; }
        public byte[] Buffer { get; }

        public DBT(byte[] buffer, Endian endian)
        {
            Buffer = buffer;
            Endian = endian;
        }

        public int EntryInfoMapOffset { get; set; }
        public int RawEntryInfoMapOffset { get; set; }

        public int SearchTableOffset { get; set; }

        public int DataMapOffset { get; set; }
        public int RawDataMapOffset { get; set; }

        public int UnkOffset4 { get; set; }

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

        public int GetIndexOfID(int id)
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
                    int entryId = sr.ReadInt32();

                    if (entryId == id)
                        return mid;

                    if (id <= entryId)
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

        public Span<byte> GetRowDataFromWhatever(ref SpanReader sr)
        {
            GetEntryData(ref sr, out Span<byte> entryDataBuffer);
            return GetRowDataFromEntryData(entryDataBuffer);
        }


        Span<byte> GetRowDataFromEntryData(Span<byte> entryData)
        {
            Span<byte> rawEntryData = entryData.Slice(1);
            byte type = (byte)(entryData[0] >> 6);
            int dataLength = RowDataLength;
            int dataIndex = entryData[0] & 0x3f;

            if (type == 0)
            {
                SpanReader sr = new SpanReader(Buffer, Endian);
                sr.Position = RawDataMapOffset + (dataIndex * dataLength);
                return sr.ReadBytes(dataLength);
            }
            else if (type == 1)
            {
                // memcpy(rowData, offs, dataLength);
                return rawEntryData.Slice(0, dataLength);
            }
            else if (type == 2)
            {
                // memcpy(rowData, (int*)((int)param_1->RawDataMapOffset + dataIndex * dataLength), dataLength);
                var sr = new SpanReader(Buffer, Endian);
                sr.Position = RawDataMapOffset + (dataIndex * dataLength);
                Span<byte> rowData = sr.ReadBytes(dataLength);

                var entryDataOffset = ((uint)dataLength >> 3) + 1 + ((0 - ((uint)dataLength & 7)) >> 31);
                for (var i = 0; i < dataLength + 1; i++)
                {
                    var val = entryData[1 + (i >> 3)];
                    val >>= i - ((i >> 3) << 3);

                    if ((val & 0x01) == 0) continue;
                    if (entryDataOffset >= entryData.Length) continue;

                    rowData[i] = entryData[(int)entryDataOffset];

                    entryDataOffset++;
                }


                return rowData;
            }

            throw new Exception("WTF?");
        }


        public int GetEntryData(ref SpanReader sr, out Span<byte> outEntryData)
        {
            byte entryDataLength = sr.Span[sr.Position];
            outEntryData = new byte[entryDataLength];
            int basePos = sr.Position;
            if (entryDataLength != 0)
            {
                int totalCount = 0;
                for (int i = 0; i < entryDataLength; i++)
                {
                    int current = totalCount >> 3;
                    if (totalCount < 0 && (totalCount & 7) != 0)
                        current++;

                    sr.Position = basePos + current + 1;

                    // Bury this and never look at it. Mystic PD shit.
                    uint val = 0;
                    if (!sr.IsEndOfSpan)
                        val += sr.ReadByte();
                    if (!sr.IsEndOfSpan)
                        val += sr.ReadByte() * 0x100u;
                    if (!sr.IsEndOfSpan)
                        val += sr.ReadByte() * 0x10000u;
                    if (!sr.IsEndOfSpan)
                        val += sr.ReadByte() * 0x1000000u;
                    val >>= totalCount - (current * 8);

                    Span<byte> b = outEntryData.Slice(i);
                    totalCount += (int)FindEntryData(val, ref b);
                }
            }
            sr.Position = basePos;
            return entryDataLength;
        }

        public uint FindEntryData(uint val, ref Span<byte> outEntryData)
        {
            SpanReader sr = new SpanReader(Buffer, Endian);
            sr.Position = (int)(RawEntryInfoMapOffset + (val & 0xFF) * 2);

            // Read the byte after the current pos
            uint next = sr.Span[sr.Position + 1]; // _local_v0_24 = (uint)local_v1_20[1];
            if (next == 0)
            {
                // Found it?
                next = FUN_00134294(val, ref outEntryData);
            }
            else
                // Not yet
                outEntryData[0] = sr.Span[sr.Position]; // *param_3 = *local_v1_20;

            // As this gets lower, we get closer to our match
            return next;
        }

        public uint FUN_00134294(uint val, ref Span<byte> buf)
        {
            int iVar10 = 0;
            uint bitNumber = 9;

            uint uVar2;
            do
            {
                uint targetIndex = (uint)((1 << ((int)bitNumber & 0x1f)) - 1 & val);
                int entryCount = ReadInt32(Buffer.AsSpan(EntryInfoMapOffset + 4), Endian);

                int max = entryCount;
                int min = -1;
                int mid;
                do
                {
                    mid = (max + min) / 2;
                    Span<byte> searchEntry = Buffer.AsSpan(SearchTableOffset + mid * 8);
                    byte bitLocation = searchEntry[0];
                    int searchIndex = ReadInt32(searchEntry.Slice(4), Endian);
                    if (searchIndex == targetIndex && bitLocation == bitNumber)
                    {
                        buf[0] = searchEntry[1]; // Key
                        return bitNumber;
                    }

                    if (bitNumber > bitLocation)
                        min = mid;
                    else if (bitNumber < bitLocation)
                        max = mid;
                    else if (bitLocation == bitNumber)
                    {
                        if (targetIndex > searchIndex)
                            min = mid;
                        else
                            max = mid;
                    }
                    mid = min + 1;

                } while (mid != max);

                uVar2 = bitNumber + 1;
                iVar10 += (uVar2 < bitNumber ? 1 : 0);
                bitNumber = uVar2;
            } while (uVar2 != 0x21 || iVar10 != 0);

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
