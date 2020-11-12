using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Buffers.Binary;
using System.Collections.ObjectModel;

using GT_SpecDB_Editor.Core.Formats;
using GT_SpecDB_Editor.Mapping;
using GT_SpecDB_Editor.Mapping.Tables;
using GT_SpecDB_Editor.Mapping.Types;
using GT_SpecDB_Editor.Utils;

using Syroot.BinaryData.Memory;
using Syroot.BinaryData;
using Syroot.BinaryData.Core;

namespace GT_SpecDB_Editor.Core
{
    public class SpecDBTable
    {
        public DBT DBT { get; set; }
        public IDI IDI { get; set; }

        public string TableName { get; set; }

        // Non original properties
        public bool IsLoaded { get; set; }
        public int LastID { get; set; }
        public SortedDictionary<int, RowData> Keys { get; set; }
        public TableMetadata TableMetadata { get; set; }
        public ObservableCollection<SpecDBRowData> Rows { get; set; }
        

        public SpecDBTable(string tableName)
        {
            TableName = tableName;
        }

        public bool IDExists(int id)
            => DBT.GetIndexOfID(id) != -1;

        /// <summary>
        /// Gets the data for a row ID.
        /// </summary>
        /// <param name="keyCode"></param>
        /// <param name="rowData"></param>
        /// <returns></returns>
        public int GetRowByCode(int keyCode, out Span<byte> rowData)
        {
            rowData = default;

            int dataLength = DBT.RowDataLength;
            int entryIndex = DBT.GetIndexOfID(keyCode);

            //if (uVar1 < *(uint *)(file + 8)) {
            if (entryIndex < DBT.EntryCount)
            {
                // ORIGINAL: iVar2 = (int)this->UnkOffset4 + *(int *)(file + uVar1 * 8 + 0x14);
                SpanReader sr = new SpanReader(DBT.Buffer, DBT.Endian);

                // Short version
                sr.Position = DBT.HeaderSize + (8 * entryIndex) + 4;
                int entryOffset = sr.ReadInt32();
                sr.Position = DBT.UnkOffset4 + entryOffset;

                if ((DBT.VersionHigh & 1) == 0)
                    rowData = sr.ReadBytes(dataLength); // memcpy(retSdbIndex,offs,dataLength);
                else
                    rowData = DBT.GetRowDataFromWhatever(ref sr);

            }

            return dataLength;
        }

        /// <summary>
        /// Gets a label by raw index (not ID).
        /// </summary>
        /// <param name="index">Index within the table.</param>
        /// <returns></returns>
        public string GetLabelByIndex(int index)
        {
            int labelCount = IDI.KeyCount;
            if (labelCount > -1 && index < labelCount)
            {
                //idi = MSpecDB::GetIDITableByIndex(pMVar2, 0);
                SpanReader sr = new SpanReader(IDI.Buffer, IDI.Endian);

                // buf = buf + *(int*)(buf + param_1 * 8 + 0x10) + *(int*)(buf + 4) * 8 + 0x12;
                sr.Position = IDI.HeaderSize + (index * 8);
                int strOffset = sr.ReadInt32();

                sr.Position = 4;
                int entryCount = sr.ReadInt32();

                sr.Position = IDI.HeaderSize + (entryCount * 8) + strOffset; // str map offset + strOffset
                sr.Position += 2; // Ignore string size as per original implementation

                /* Original Returns the length of the string (pointer) and the buffer
                strncpy(strOut, buf, strBufLen);
                strOut[strBufLen + -1] = '\0';
                iVar1 = strlen(strOut, (char*)buf);
                * return iVar1; */

                return sr.ReadString0(); // Null-Terminated
            }

            return null;
        }

        /// <summary>
        /// Gets the ID/Code of a label.
        /// </summary>
        /// <param name="label">Label name.</param>
        /// <returns></returns>
        public int GetIDOfLabel(string label)
            => IDI.GetIDOfLabel(label);

        #region Initializers

        public void ReadDBTMapOffsets(SpecDB specDb)
        {
            //if (TableName.Equals("CAR_NAME_"))
            //    TableName += specDb.LocaleName;

            var buffer = File.ReadAllBytes(Path.Combine(specDb.FolderName, TableName) + ".dbt");
            SpanReader sr = new SpanReader(buffer);

            var magic = sr.ReadStringRaw(4);
            if (magic != "GTDB")
                throw new InvalidDataException("DBT Table had invalid magic.");

            ushort versionHigh = sr.ReadUInt16();
            switch (versionHigh)
            {
                case 0x0001:
                case 0x0003:
                case 0x0103:
                case 0x0004:
                case 0x0104:
                    sr.Endian = Endian.Little; break;
                case 0x0500:
                case 0x0700:
                case 0x0800:
                case 0x0801:
                    sr.Endian = Endian.Big; break;
            }
            DBT = new DBT(buffer, sr.Endian);

            sr.Position = 4;
            versionHigh = sr.ReadUInt16();
            sr.Position += 2;
            uint entryCount = sr.ReadUInt32();

            if (sr.Length <= 32)
                return;

            sr.Position = (int)(DBT.HeaderSize + (entryCount * 8));
            DBT.EntryInfoMapOffset = sr.Position;
            if ((versionHigh & 1) != 0)
            {
                DBT.RawEntryInfoMapOffset = sr.Position + 0x08;
                DBT.SearchTableOffset = sr.Position + 0x208;

                sr.Position += sr.ReadInt32();
                DBT.DataMapOffset = sr.Position;
                DBT.RawDataMapOffset = sr.Position + 0x08;

                DBT.UnkOffset4 = sr.Position + sr.ReadInt32();
            }
            else
                DBT.UnkOffset4 = sr.Position;
        }

        public void ReadIDIMapOffsets(SpecDB specDb)
        {
            //if (TableName.Equals("CAR_NAME_"))
            //    TableName += specDb.LocaleName;

            var buffer = File.ReadAllBytes(Path.Combine(specDb.FolderName, TableName) + ".idi");
            SpanReader sr = new SpanReader(buffer);

            var magic = sr.ReadStringRaw(4);
            if (magic != "GTID")
                throw new InvalidDataException("IDI Table had invalid magic.");

            Endian endian = sr.ReadByte() != 0 ? Endian.Little : Endian.Big;

            IDI = new IDI(buffer, endian);
        }

        #endregion

        // Non original functions

        public void LoadAllRows(SpecDB db)
        {
            LoadAllRowKeys();
            LoadMetadata(db);
            LoadAllRowData();
            PopulateRowStringsIfNeeded(db);
            IsLoaded = true;
        }

        private void LoadAllRowKeys()
        {

            // Make a list of all the keys the IDI contains. IDI sometimes have keys without data, so we need it to then filter the dbt keys.
            SpanReader idiReader = new SpanReader(IDI.Buffer, IDI.Endian);
            Dictionary<int, string> idsToLabels = new Dictionary<int, string>();
            int idiKeyCount = IDI.KeyCount;
            for (int i = 0; i < idiKeyCount; i++)
            {
                idiReader.Position = IDI.HeaderSize + (i * 0x08);
                int labelOffset = idiReader.ReadInt32();
                int id = idiReader.ReadInt32();

                idiReader.Position = IDI.HeaderSize + (idiKeyCount * 0x08) + labelOffset;
                idiReader.Position += 2; // Ignore string length
                string label = idiReader.ReadString0();
                idsToLabels.Add(id, label);
            }


            // Register all our keys that actually have data now.
            SpanReader dbtReader = new SpanReader(DBT.Buffer, DBT.Endian);
            Keys = new SortedDictionary<int, RowData>();

            int keyCount = DBT.EntryCount;
            for (int i = 0; i < keyCount; i++)
            {
                dbtReader.Position = DBT.HeaderSize + (i * 0x08);
                int id = dbtReader.ReadInt32();
                
                if (idsToLabels.TryGetValue(id, out string label)) // If it doesn't, its a key that has no data according to IDI
                    Keys.Add(id, new RowData() { Id = id, Label = label });
            }

            LastID = Keys.Last().Key;
        }

        public int DumpTable(string path)
        {
            SpanReader idiReader = new SpanReader(IDI.Buffer, IDI.Endian);
            Dictionary<int, string> idsToLabels = new Dictionary<int, string>();
            int idiKeyCount = IDI.KeyCount;
            for (int i = 0; i < idiKeyCount; i++)
            {
                idiReader.Position = IDI.HeaderSize + (i * 0x08);
                int labelOffset = idiReader.ReadInt32();
                int id = idiReader.ReadInt32();

                idiReader.Position = IDI.HeaderSize + (idiKeyCount * 0x08) + labelOffset;
                idiReader.Position += 2; // Ignore string length
                string label = idiReader.ReadString0();
                idsToLabels.Add(id, label);
            }


            // Register all our keys that actually have data now.
            SpanReader dbtReader = new SpanReader(DBT.Buffer, DBT.Endian);
            var keys = new SortedDictionary<int, RowData>();

            int keyCount = DBT.EntryCount;
            for (int i = 0; i < keyCount; i++)
            {
                dbtReader.Position = DBT.HeaderSize + (i * 0x08);
                int id = dbtReader.ReadInt32();

                if (idsToLabels.TryGetValue(id, out string label)) // If it doesn't, its a key that has no data according to IDI
                    keys.Add(id, new RowData() { Id = id, Label = label });
            }

            using (var sw = new StreamWriter(path))
            {
                foreach (var key in keys)
                {
                    GetRowByCode(key.Key, out Span<byte> rowData);
                    sw.WriteLine($"{key.Value.Label} ({key.Key}) | {BitConverter.ToString(rowData.ToArray())}");
                }
            }

            return keys.Count;
        }

        private void LoadMetadata(SpecDB db)
        {
            if (TableName.StartsWith("CAR_NAME_"))
                TableMetadata = new CarName(db.SpecDBName, TableName.Split('_')[2]);
            else
            {
                switch (TableName)
                {
                    case "AIR_CLEANER":
                        TableMetadata = new AirCleaner(db.SpecDBName); break;
                    case "ARCADEINFO_NORMAL":
                        TableMetadata = new ArcadeInfoNormal(db.SpecDBName); break;
                    case "BRAKE":
                        TableMetadata = new Brake(db.SpecDBName); break;
                    case "CATALYST":
                        TableMetadata = new Catalyst(db.SpecDBName); break;
                    case "CLUTCH":
                        TableMetadata = new Clutch(db.SpecDBName); break;
                    case "COMPUTER":
                        TableMetadata = new Computer(db.SpecDBName); break;
                    case "COURSE":
                        TableMetadata = new Course(db.SpecDBName); break;
                    case "CAR_CUSTOM_INFO":
                        TableMetadata = new CarCustomInfo(db.SpecDBName); break;
                    case "DEFAULT_PARAM":
                        TableMetadata = new DefaultParam(db.SpecDBName); break;
                    case "ENGINE":
                        TableMetadata = new Engine(db.SpecDBName); break;
                    case "EXHAUST_MANIFOLD":
                        TableMetadata = new ExhaustManifold(db.SpecDBName); break;
                    case "FLYWHEEL":
                        TableMetadata = new Flywheel(db.SpecDBName); break;
                    case "GEAR":
                        TableMetadata = new Gear(db.SpecDBName); break;
                    case "MAKER":
                        TableMetadata = new Maker(db.SpecDBName); break;
                    case "MODEL_INFO":
                        TableMetadata = new ModelInfo(db.SpecDBName); break;
                    case "PAINT_COLOR_INFO":
                        TableMetadata = new PaintColorInfo(db.SpecDBName); break;
                    case "GENERIC_CAR":
                        TableMetadata = new GenericCar(db.SpecDBName); break;
                    case "FRONTTIRE":
                        TableMetadata = new FrontTire(db.SpecDBName); break;
                    case "REARTIRE":
                        TableMetadata = new RearTire(db.SpecDBName); break;
                    case "RACINGMODIFY":
                        TableMetadata = new RacingModify(db.SpecDBName); break;
                    default:
                        throw new NotSupportedException("This table is not yet mapped.");
                }
            }
        }

        private void LoadAllRowData()
        {
            Rows = new ObservableCollection<SpecDBRowData>();
            foreach (var key in Keys)
            {
                GetRowByCode(key.Key, out Span<byte> rowData);
                SpecDBRowData row = TableMetadata.ReadRow(rowData, DBT.Endian);
                row.ID = key.Key;
                row.Label = key.Value.Label;
                Rows.Add(row);
            }
        }

        private void PopulateRowStringsIfNeeded(SpecDB db)
        {
            foreach (var row in Rows)
            {
                foreach (var dataType in row.ColumnData)
                {
                    if (dataType is DBString str)
                    {
                        // Lazy load
                        if (!db.StringDatabases.TryGetValue(str.FileName, out StringDatabase strDb))
                        {
                            var newStrDb = StringDatabase.LoadFromFile(Path.Combine(db.FolderName, str.FileName));
                            db.StringDatabases.Add(str.FileName, newStrDb);
                            strDb = newStrDb;
                        }

                        str.Value = strDb.Strings[str.StringIndex];
                    }
                }
            }
        }

        public void SaveTable(SpecDB db, string outputDir)
        {
            // Write the IDI first
            SaveIDTable(Path.Combine(outputDir, $"{TableName}.idi"));

            // Write the DBT
            SaveDBTable(Path.Combine(outputDir, $"{TableName}.dbt"));

            // Write linked String Databases
            var savedStringDatabases = new HashSet<string>();
            foreach (var meta in TableMetadata.Columns)
            {
                if (meta.ColumnType != DBColumnType.String)
                    continue;

                if (!savedStringDatabases.Contains(meta.StringFileName))
                {
                    SaveStringDatabase(db.StringDatabases[meta.StringFileName], Path.Combine(outputDir, meta.StringFileName));
                    savedStringDatabases.Add(meta.StringFileName);
                }
            }
        }

        private void SaveIDTable(string outputPath)
        {
            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var bs = new BinaryStream(fs, IDI.Endian == Endian.Big ? ByteConverter.Big : ByteConverter.Little))
            {
                bs.WriteString("GTID", StringCoding.Raw);
                bs.WriteInt32(Rows.Count);
                bs.WriteInt32(0);
                bs.WriteInt32(41);
                var orderedRows = Rows.OrderBy(e => e.Label.Length)
                    .ThenBy(e => e.Label)
                    .ToList();

                var strTable = GetLabelTable(orderedRows);

                // Write strings
                bs.Position = 0x10 + (Rows.Count * 8);
                strTable.SaveStream(bs);
                bs.Position = 0x10;

                foreach (var row in orderedRows)
                {
                    bs.WriteInt32(strTable.GetStringOffset(row.Label));
                    bs.WriteInt32(row.ID);
                }

            }
        }

        private void SaveDBTable(string outputPath)
        {
            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var bs = new BinaryStream(fs, DBT.Endian == Endian.Big ? ByteConverter.Big : ByteConverter.Little))
            {
                bs.WriteString("GTDB", StringCoding.Raw);
                bs.WriteInt16(8);
                bs.WriteInt16(8);
                bs.WriteInt32(Rows.Count);

                int columnSize = TableMetadata.GetColumnSize();
                bs.WriteInt32(columnSize);

                int rowDataOffset = DBT.HeaderSize + (Rows.Count * 8);
                for (int i = 0; i < Rows.Count; i++)
                {
                    bs.Position = DBT.HeaderSize + (i * 8);
                    SpecDBRowData row = Rows[i];
                    bs.WriteInt32(row.ID);

                    int rowRelativeOffset = i * columnSize;
                    bs.WriteInt32(rowRelativeOffset);
                    bs.Position = rowDataOffset + rowRelativeOffset;
                    foreach (var data in row.ColumnData)
                        data.Serialize(bs);
                }
            }
        }

        private void SaveStringDatabase(StringDatabase strDb, string outputPath)
        {
            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var bs = new BinaryStream(fs, strDb.Endian == Endian.Big ? ByteConverter.Big : ByteConverter.Little))
            {
                bs.WriteString("GTST", StringCoding.Raw);
                bs.WriteInt32(strDb.Strings.Count);
                bs.WriteInt32(strDb.Version);
                bs.Position += 0x4;

                long lastDataPos = SDB.HeaderSize + (strDb.Strings.Count * sizeof(uint));
                var strTable = GetStringDbTable(strDb.Strings);
                bs.Position = SDB.HeaderSize + (strDb.Strings.Count * sizeof(uint));
                strTable.SaveStream(bs);

                for (int i = 0; i < strDb.Strings.Count; i++)
                {
                    bs.Position = SDB.HeaderSize + (i * sizeof(uint));
                    bs.WriteInt32(strTable.GetStringOffset(strDb.Strings[i]));
                }
            }
        }

        private OptimizedStringTable GetLabelTable(List<SpecDBRowData> rows)
        {
            var optimizedStringTable = new OptimizedStringTable();
            optimizedStringTable.StringCoding = StringCoding.Int16CharCount;
            optimizedStringTable.NullTerminated = true;
            optimizedStringTable.Alignment = 0x02;
            optimizedStringTable.IsRelativeOffsets = true;

            foreach (var row in rows)
                optimizedStringTable.AddString(row.Label);

            return optimizedStringTable;
        }

        private OptimizedStringTable GetStringDbTable(IEnumerable<string> strings)
        {
            var optimizedStringTable = new OptimizedStringTable();
            optimizedStringTable.StringCoding = StringCoding.Int16CharCount;
            optimizedStringTable.NullTerminated = true;
            optimizedStringTable.Alignment = 0x02;
            optimizedStringTable.IsRelativeOffsets = true;

            foreach (var str in strings)
                optimizedStringTable.AddString(str);

            return optimizedStringTable;
        }
    }
}
