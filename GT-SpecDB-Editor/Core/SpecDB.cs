using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Syroot.BinaryData;
using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GT_SpecDB_Editor.Mapping;
using GT_SpecDB_Editor.Mapping.Types;
using GT_SpecDB_Editor.Core.Formats;

namespace GT_SpecDB_Editor.Core
{
    public class SpecDB
    {
        public string FolderName { get; set; }
        public string SpecDBName { get; set; }

        /// <summary>
        /// Tables for this spec db.
        /// </summary>
        public Dictionary<string, SpecDBTable> Tables { get; set; }

        /// <summary>
        /// Switch as whether we are loading tables like the game does.
        /// If not specialized such as the purpose of this program.
        /// </summary>
        public bool LoadingAsOriginalImplementation { get; }

        public const int SPEC_DB_TABLE_COUNT = 44;
        /// <summary>
        /// All tables that should be loaded as per original implementation.
        /// </summary>
        public SpecDBTable[] Fixed_Tables { get; }
        public SDB UniversalStringDatabase { get; set; }
        public SDB LocaleStringDatabase { get; set; }
        public string LocaleName { get; set; } = "british"; // Change this accordingly.
        public Dictionary<string, StringDatabase> StringDatabases = new Dictionary<string, StringDatabase>();

        /// <summary>
        /// Used to keep track of how different specdbs are read
        /// </summary>
        private HashSet<string> SpecDBs = new HashSet<string>()
        {
            "GT4_PROLOGUE_EU1110",  // GT4P
            "GT4_CN2560",           // GT4 China
            "GT4_US2560",           // GT4
            "GT4_PREMIUM_US2560",   // GT4O
            "TT_EU2630",            // Tourist Trophy
            "GT5_TRIAL_EU2704",     // GTHD
            "GT5_PROLOGUE2813",     // GT5P
            "GT_PSP_JP2817",        // GTPSP
            "GT5_ACADEMY_09_2900",  // Gran Turismo 5 Time Trial Challenge
            "GT5_JP2904",           // GT5 Kiosk
            "GT5_PREVIEWJP2904",    // GT5 Kiosk
            "GT5_JP3009",           // GT5 Retail
            "GT5_JP3010",           // GT5 - 1.05+
        };

        /// <summary>
        /// List of all tables used by the game
        /// </summary>
        private readonly string[] TABLE_NAMES = Enum.GetNames(typeof(SpecDBTables));

        /* Code: ID of the column.
         * Label: name of the row i.e: _117_coupe_68. 
         */

        public SpecDB(string folderName, bool loadAsOriginalImplementation)
        {
            FolderName = folderName;
            SpecDBName = Path.GetFileNameWithoutExtension(folderName);
            LoadingAsOriginalImplementation = loadAsOriginalImplementation;

            if (LoadingAsOriginalImplementation)
            {
                Fixed_Tables = new SpecDBTable[SPEC_DB_TABLE_COUNT];
                for (int i = 0; i < Fixed_Tables.Length; i++)
                    Fixed_Tables[i] = new SpecDBTable(TABLE_NAMES[i]);
            }
            else
            {
                Tables = new Dictionary<string, SpecDBTable>();
            }
        }

        public static SpecDB LoadFromSpecDBFolder(string folderName, bool loadAsOriginalImplementation)
        {
            if (!Directory.Exists(folderName))
                throw new DirectoryNotFoundException("SpecDB directory is not found.");

            SpecDB db = new SpecDB(folderName, loadAsOriginalImplementation);
            if (db.LoadingAsOriginalImplementation)
            {
                db.ReadAllTables();
                db.ReadStringDatabases();
            }
            else
            {
                db.PreLoadAllTablesFromCurrentFolder();
            }
            
            return db;
        }

        private void ReadStringDatabases()
        {
            UniversalStringDatabase = ReadStringDatabase("UnistrDB.sdb");
            LocaleStringDatabase = ReadStringDatabase($"{LocaleName}_StrDB.sdb");
        }

        private SDB ReadStringDatabase(string name)
        {
            byte[] sdbFile = File.ReadAllBytes(Path.Combine(FolderName, name));
            SpanReader sr = new SpanReader(sdbFile);
            sr.Position = 0x08;
            Endian endian = sr.ReadInt16() == 1 ? Endian.Little : Endian.Big;
            SDB sdb = new SDB(sdbFile, endian);
            
            return sdb;
        }

        public bool KeyExistsAtTable(int keyCode, int tableID)
        {
            if (tableID > Fixed_Tables.Length)
                return false;

            var table = Fixed_Tables[tableID];
            return table.IDExists(keyCode);
        }

        private void ReadAllTables()
        {
            for (int i = 0; i < TABLE_NAMES.Length; i++)
            {
                Fixed_Tables[i].ReadDBTMapOffsets(this);
                Fixed_Tables[i].ReadIDIMapOffsets(this);
            }
        }

        /// <summary>
        /// Gets the actual full name of a car by its code.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public string GetCarName(string label)
        {
            int rowID = GetIDOfCarLabel(label);
            if (TryGetCarNameStringIDOfCarID(rowID, out int stringIndex))
                return LocaleStringDatabase.GetStringByID(stringIndex);

            return null;
        }

        /// <summary>
        /// Gets the shortened car name of a car by its label.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public string GetCarShortName(string label)
        {
            int rowID = GetIDOfCarLabel(label);
            if (TryGetCarShortNameStringIDOfCarID(rowID, out int stringIndex))
                return LocaleStringDatabase.GetStringByID(stringIndex);

            return null;
        }

        public List<string> GetCarLabelList()
        {
            List<string> labels = new List<string>();
            int labelCount = GetCarLabelCount();
            for (int i = 0; i < labelCount; i++)
            {
                string label = GetCarLabelByIndex(i);
                labels.Add(label);
            }

            return labels;
        }

        /// <summary>
        /// Gets the ID of a car by its label.
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public int GetIDOfCarLabel(string label)
            => GetIDOfLabelFromTable(SpecDBTables.GENERIC_CAR, label);


        /// <summary>
        /// Gets the row data from a table by ID.
        /// </summary>
        /// <param name="table"></param>
        /// <param name="keyCode"></param>
        /// <param name="rowData"></param>
        /// <returns></returns>
        public int GetRowFromTable(SpecDBTables table, int keyCode, out Span<byte> rowData)
        {
            rowData = default;
            if (Fixed_Tables[(int)table] != null)
                return Fixed_Tables[(int)table].GetRowByCode(keyCode, out rowData);

            return 0;
        }

        /// <summary>
        /// Gets the ID of a row by its label.
        /// </summary>
        /// <param name="table">Table to look at.</param>
        /// <param name="label">Label name.</param>
        /// <returns></returns>
        public int GetIDOfLabelFromTable(SpecDBTables table, string label)
        {
            if ((int)table >= 0 && (int)table < Fixed_Tables.Length)
                return Fixed_Tables[(int)table].GetIDOfLabel(label);

            return -1;
        }

        public int GetCarLabelCount()
        {
            IDI idTable = Fixed_Tables[(int)SpecDBTables.GENERIC_CAR].IDI;
            return idTable.KeyCount;
        }

        public int GetLabelCountForTable(SpecDBTables table)
        {
            IDI idTable = Fixed_Tables[(int)table].IDI;
            return idTable.KeyCount;
        }

        /// <summary>
        /// Gets a car code by raw index (not ID).
        /// </summary>
        /// <param name="index">Index within the table.</param>
        /// <returns></returns>
        public string GetCarLabelByIndex(int index)
        {
            int carLabelCount = GetLabelCountForTable(SpecDBTables.GENERIC_CAR);
            if (carLabelCount > -1 && index < carLabelCount)
            {
                //idi = MSpecDB::GetIDITableByIndex(pMVar2, 0);
                IDI idTable = Fixed_Tables[(int)SpecDBTables.GENERIC_CAR].IDI;
                SpanReader sr = new SpanReader(idTable.Buffer);

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
        /// Gets the offset of the string key for a row code in the IDI.
        /// </summary>
        /// <param name="table">Table to look at.</param>
        /// <param name="code">Code of the row.</param>
        /// <returns></returns>
        public int GetLabelOffsetByIDFromTable(SpecDBTables table, int code)
        {
            SpecDBTable sTable = Fixed_Tables[(int)table];
            IDI idi = sTable.IDI;

            SpanReader sr = new SpanReader(idi.Buffer);
            sr.Position = 4;
            int entryCount = sr.ReadInt32();

            // "original" implementation had one while and one do loop, probably decompiler that just failed
            for (int i = 0; i < entryCount; i++)
            {
                sr.Position = IDI.HeaderSize + (i * 8) + 4;
                int entryCode = sr.ReadInt32();
                if (entryCode == code)
                {
                    // Original: return (char*)(idiFile + index * 8 + *(int*)(iVar3 * 8 + idiFile + 0x10) + 0x12);

                    // *(int*)(iVar3 * 8 + idiFile + 0x10)
                    int entryPos = IDI.HeaderSize + (i * 8);
                    sr.Position = entryPos;
                    int stringOffset = sr.ReadInt32();

                    // idiFile + index * 8 (go to the beginning of the second table)
                    sr.Position = IDI.HeaderSize + entryCount * 8; // Header is added due to below

                    // Add the two
                    sr.Position += stringOffset;

                    //0x12 is just the base header + the string length as short, optimized
                    return sr.Position + 2;
                }
            }

            return -1; // NULL
        }
        
        /// <summary>
        /// Gets the string db index (actual name) of a car ID within the string database.
        /// </summary>
        /// <param name="carCode"></param>
        /// <param name="stringIndex"></param>
        /// <returns></returns>
        private bool TryGetCarNameStringIDOfCarID(int carCode, out int stringIndex)
        {
            stringIndex = 0;

            int dataLength = Fixed_Tables[(int)SpecDBTables.CAR_NAME_].GetRowByCode(carCode, out Span<byte> rowData);
            if (dataLength != 0)
            {
                stringIndex = BinaryPrimitives.ReadInt32LittleEndian(rowData);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets the string db index (short name) of a car ID within the string database.
        /// </summary>
        /// <param name="carCode"></param>
        /// <param name="stringIndex"></param>
        /// <returns></returns>
        private bool TryGetCarShortNameStringIDOfCarID(int carCode, out int stringIndex)
        {
            stringIndex = 0;

            int dataLength = Fixed_Tables[(int)SpecDBTables.CAR_NAME_].GetRowByCode(carCode, out Span<byte> rowData);
            if (dataLength != 0)
            {
                stringIndex = BinaryPrimitives.ReadInt32LittleEndian(rowData.Slice(4));
                return true;
            }

            return false;
        }

        // Non Original Implementations
        public void PreLoadAllTablesFromCurrentFolder()
        {
            var tablePaths = Directory.GetFiles(FolderName, "*.dbt", SearchOption.TopDirectoryOnly);
            foreach (var table in tablePaths)
            {
                string tableName = Path.GetFileNameWithoutExtension(table);
                if (!File.Exists(Path.Combine(FolderName, tableName) + ".idi"))
                    continue;

                var specdbTable = new SpecDBTable(tableName);
                specdbTable.ReadDBTMapOffsets(this);
                specdbTable.ReadIDIMapOffsets(this);
                Tables.Add(specdbTable.TableName, specdbTable);
            }
        }

        public void SavePartsInfo(string folder)
        {
            // TBI and TBD are both linked. We need to save them simultaneously
            var carTable = Tables["GENERIC_CAR"];
            if (!carTable.IsLoaded)
                carTable.LoadAllRows(this);

            var defaultParts = Tables["DEFAULT_PARTS"];
            if (!defaultParts.IsLoaded)
                defaultParts.LoadAllRows(this);

            using (var tbdWriter = new BinaryStream(new FileStream(Path.Combine(folder, "PartsInfo.tbd"), FileMode.Create)))
            using (var tbiWriter = new BinaryStream(new FileStream(Path.Combine(folder, "PartsInfo.tbi"), FileMode.Create)))
            {
                tbdWriter.ByteConverter = carTable.DBT.Endian == Endian.Big ? ByteConverter.Big : ByteConverter.Little;
                tbiWriter.ByteConverter = tbdWriter.ByteConverter;

                // We need to iterate through all the cars to save all of their linked parts
                for (int i = 0; i < carTable.Rows.Count; i++)
                {
                    // Begin to write the stride index
                    int fieldCountOffset = (int)tbdWriter.Position;
                    int fieldsWritten = 0;
                    tbdWriter.Position += 4;
                    tbdWriter.Align(0x08, true);

                    SpecDBRowData car = carTable.Rows[i];

                    int defaultPartsID = (car.ColumnData[1] as DBInt).Value;
                    SpecDBRowData df = defaultParts.Rows.FirstOrDefault(e => e.ID == defaultPartsID);

                    // Iterate through each part table
                    int lastTableID = 0;
                    for (int j = 0; j < df.ColumnData.Count; j += 2)
                    {
                        int tableID = (df.ColumnData[j] as DBInt).Value;
                        if (tableID > 32)
                            continue;

                        // When the row is -1 for its table id, use a dirty trick and use the last table ID
                        if (tableID == -1)
                            tableID = lastTableID + 1;
                        lastTableID = tableID;

                        if (lastTableID >= 32)
                            break; // We are done pretty much

                        int partRowID = (df.ColumnData[j + 1] as DBInt).Value;

                        // Get our table by said ID
                        SpecDBTable partTable = Tables.Values.FirstOrDefault(table => table.TableID == tableID);

                        // Ignored tables, these may contain data but they are not kept in mind
                        if (partTable.TableName == "NOS")
                            continue;

                        if (!partTable.IsLoaded)
                            partTable.LoadAllRows(this);

                        SpecDBRowData mainRow = partTable.Rows.FirstOrDefault(e => e.ID == partRowID);
                        // Sometimes the main ID points to a generic part. We have to include it if its not a regular car label.
                        if (partRowID != -1 && (mainRow != null && !mainRow.Label.Contains(car.Label)))
                        {
                            var colMeta = partTable.TableMetadata.Columns.Find(e => e.ColumnName.Equals("category", StringComparison.OrdinalIgnoreCase));
                            int cat = (mainRow.ColumnData[colMeta.ColumnIndex] as DBByte).Value;

                            WriteTBDField(tbdWriter, mainRow, tableID, cat);
                            fieldsWritten++;
                        }

                        // Register all alt parts for it.
                        string rowFilter = $"{partTable.TableMetadata.LabelPrefix}{car.Label}";
                        foreach (SpecDBRowData partRow in partTable.Rows)
                        {
                            if (!partRow.Label.StartsWith(rowFilter))
                                continue;

                            /*
                            if ((!car.Label.Contains("_std") && partRow.Label.Contains("_std")) // Filter STD parts if it isn't one
                                || (!car.Label.Contains("_rm") && partRow.Label.Contains("_rm")) // Filter RM parts if it isn't one
                                || (!car.Label.Contains("_ac") && partRow.Label.Contains("_ac")) // Filter Academy if it isn't one
                                || (!car.Label.Contains("_cf") && partRow.Label.Contains("_cf"))) // Filter Stealth (?) if it isn't one
                                continue;
                                */
                            if (partRow.Label.Length - rowFilter.Length > 4)
                                continue; // Assume its a different car

                            var colMeta = partTable.TableMetadata.Columns.Find(e => e.ColumnName.Equals("category", StringComparison.OrdinalIgnoreCase));
                            int cat = (partRow.ColumnData[colMeta.ColumnIndex] as DBByte).Value;

                            // Apparently NATUNE Stage 0 is ignored
                            if (partTable.TableName == "NATUNE" && cat == 0)
                                continue;

                            WriteTBDField(tbdWriter, partRow, tableID, cat);
                            fieldsWritten++;
                        }
                    }

                    // We are done writing the fields, write the TBI metadata now
                    tbiWriter.Position = TBI.HeaderSize + (i * 0x10);
                    tbiWriter.WriteInt32(car.ID);
                    tbiWriter.WriteInt32(fieldCountOffset); // Data Start
                    tbiWriter.WriteInt32((int)tbdWriter.Position - fieldCountOffset); // Data Length
                    tbiWriter.Align(0x08, grow: true);

                    // Write the TBD entry field count
                    using (Seek seek = tbdWriter.TemporarySeek(fieldCountOffset, SeekOrigin.Begin))
                        tbdWriter.WriteInt32(fieldsWritten);
                    
                }
            }
        }

        private void WriteTBDField(BinaryStream tbdWriter, SpecDBRowData partRow, int tableID, int fieldNumber)
        {
            tbdWriter.WriteInt32(tableID);
            tbdWriter.WriteInt32(partRow.ID);
            tbdWriter.WriteInt32(fieldNumber); // In general it seems like the row "category" is written
            tbdWriter.Align(0x08, true);
        }

        public enum SpecDBTables
        {
            GENERIC_CAR,
            BRAKE,
            BRAKECONTROLLER,
            SUSPENSION,
            ASCC,
            TCSC,
            CHASSIS,
            RACINGMODIFY,
            LIGHTWEIGHT,
            STEER,
            DRIVETRAIN,
            GEAR,
            ENGINE,
            NATUNE,
            TURBINEKIT,
            PORTPOLISH,
            ENGINEBALANCE,
            DISPLACEMENT,
            COMPUTER,
            INTERCOOLER,
            MUFFLER,
            CLUTCH,
            FLYWHEEL,
            PROPELLERSHAFT,
            LSD,
            FRONTTIRE,
            REARTIRE,
            NOS,
            SUPERCHARGER,
            WHEEL,
            WING,
            TIRESIZE,
            TIRECOMPOUND,
            TIREFORCEVOL,
            COURSE,
            RACE,
            DEFAULT_PARTS,
            DEFAULT_PARAM,
            ENEMY_CARS,
            CAR_NAME_,
            VARIATION,
            MODEL_INFO,
            MAKER,
            TUNER,
        }
    }
}
