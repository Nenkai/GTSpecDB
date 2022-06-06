using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data.SQLite;

using GTSpecDB.Core;
using GTSpecDB.Mapping;
using GTSpecDB.Mapping.Types;

namespace GTSpecDB.Sqlite
{
    public class SQLiteImporter
    {
        private SpecDB _db;
        public bool ImportRaces { get; set; }

        public SQLiteImporter()
        {
            _db = new SpecDB();
        }

        public void Import(string sqliteFile, string outputDirectory)
        {
            using (var m_dbConnection = new SQLiteConnection($"Data Source={sqliteFile};Version=3;"))
            {
                m_dbConnection.Open();

                FetchDatabaseName(m_dbConnection);

                CreateTables(m_dbConnection);

                SetupColumnTypes(m_dbConnection);

                PopulateTableRows(m_dbConnection);
            }

            Console.WriteLine("Database imported. Exporting to SpecDB format..");

            foreach (var table in _db.Tables)
            {
                Console.WriteLine($"Saving table '{table.Key}'");
                table.Value.SaveTable(_db, Path.Combine(outputDirectory, _db.SpecDBFolderType.ToString()));
            }
        }

        private void FetchDatabaseName(SQLiteConnection conn)
        {
            var command = new SQLiteCommand("SELECT * FROM _DatabaseInfo", conn);
            var reader = command.ExecuteReader();

            reader.Read();
            _db.FolderName = reader.GetString(0);

            if (!Enum.TryParse(_db.FolderName, out SpecDBFolder folderType))
                throw new Exception($"Unable to parse folder type {_db.FolderName} as a proper specdb type.");

            _db.SpecDBFolderType = folderType;
        }

        private void CreateTables(SQLiteConnection conn)
        {
            var command = new SQLiteCommand("SELECT * FROM _DatabaseTableInfo", conn);
            var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string tableName = reader.GetString(0);
                int tableID = reader.GetInt32(1);
                bool bigEndian = reader.GetInt32(2) == 1;

                SpecDBTable table = new SpecDBTable(tableName);
                table.TableID = tableID;
                table.BigEndian = bigEndian;

                _db.Tables.Add(tableName, table);
            }
        }

        private void SetupColumnTypes(SQLiteConnection conn)
        {
            foreach (var table in _db.Tables)
            {
                var command = new SQLiteCommand($"SELECT * FROM {table.Key}_typeinfo", conn);
                var reader = command.ExecuteReader();

                table.Value.TableMetadata = new SQLiteTableMetadata();

                reader.Read();
                List<string> colNames = new List<string>(reader.FieldCount);
                for (var i = 0; i < reader.FieldCount; i++)
                    colNames.Add(reader.GetString(i));

                reader.Read();
                List<string> colTypes = new List<string>(reader.FieldCount);
                for (var i = 0; i < reader.FieldCount; i++)
                    colTypes.Add(reader.GetString(i));


                for (var i = 0; i < colNames.Count; i++)
                {
                    if (!Enum.TryParse(colTypes[i], out DBColumnType colType))
                    {

                    }

                    var colMeta = new ColumnMetadata(colNames[i], colType);
                    if (colType == DBColumnType.String)
                    {
                        colMeta.StringFileName = GetSDBNameFromTableName(table.Value);
                    }

                    table.Value.TableMetadata.Columns.Add(colMeta);
                }
            }
        }

        private void PopulateTableRows(SQLiteConnection conn)
        {
            foreach (var table in _db.Tables)
            {
                var command = new SQLiteCommand($"SELECT * FROM {table.Key}", conn);
                var reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var row = new SpecDBRowData();
                    row.ID = reader.GetInt32(0);
                    row.Label = reader.GetString(1);

                    int colNum = table.Value.TableMetadata.Columns.Count;
                    row.ColumnData = new List<IDBType>(colNum);

                    for (var colIndex = 0; colIndex < colNum; colIndex++)
                    {
                        ColumnMetadata col = table.Value.TableMetadata.Columns[colIndex];

                        IDBType type;
                        switch (col.ColumnType)
                        {
                            case DBColumnType.Bool:
                               type = new DBBool(reader.GetBoolean(2 + colIndex));
                                break;
                            case DBColumnType.Byte:
                                type = new DBByte(reader.GetByte(2 + colIndex));
                                break;
                            case DBColumnType.SByte:
                                type = new DBSByte((sbyte)reader.GetInt32(2 + colIndex));
                                break;
                            case DBColumnType.Short:
                                type = new DBShort(reader.GetInt16(2 + colIndex));
                                break;
                            case DBColumnType.UShort:
                                type = new DBUShort((ushort)reader.GetInt32(2 + colIndex));
                                break;
                            case DBColumnType.Int:
                                type = new DBInt(reader.GetInt32(2 + colIndex));
                                break;
                            case DBColumnType.UInt:
                                type = new DBUInt((uint)reader.GetInt64(2 + colIndex));
                                break;
                            case DBColumnType.String:
                                string str = reader.GetString(2 + colIndex);
                                type = AddNewString(table.Value, str);
                                break;
                            case DBColumnType.Long:
                                type = new DBLong(reader.GetInt64(2 + colIndex));
                                break;
                            case DBColumnType.Float:
                                type = new DBFloat(reader.GetFloat(2 + colIndex));
                                break;
                            default:
                                throw new Exception("Unexpected type");
                                break;
                        }

                        row.ColumnData.Add(type);
                    }

                    table.Value.Rows.Add(row);
                }
            }
        }

        private DBString AddNewString(SpecDBTable table, string str)
        {
            string sdbName = GetSDBNameFromTableName(table);

            if (!_db.StringDatabases.TryGetValue(sdbName, out StringDatabase strDb))
            {
                strDb = new StringDatabase();
                _db.StringDatabases.TryAdd(sdbName, strDb);
            }

            if (!strDb.Strings.Contains(str))
                strDb.Strings.Add(str);

            int idx = strDb.Strings.IndexOf(str);
            return new DBString(idx, sdbName);
        }

        private string GetSDBNameFromTableName(SpecDBTable table)
        {
            if (table.TableName.Contains("CAR_NAME") ||
                table.TableName.Contains("CAR_VARIATION") ||
                table.TableName.Contains("COURSE_NAME") ||
                table.TableName.Contains("VARIATION") ||
                table.TableName.Contains("RIDER_EQUIPMENT"))
            {
                string sdbName;
                if (table.TableName.Contains("american"))
                {
                    sdbName = "american_StrDB.sdb";
                }
                else if (table.TableName.Contains("japanese"))
                {
                    sdbName = "japanese_StrDB.sdb";
                }
                else if (table.TableName.Contains("german"))
                {
                    sdbName = "german_StrDB.sdb";
                }
                else if (table.TableName.Contains("french"))
                {
                    sdbName = "french_StrDB.sdb";
                }
                else if (table.TableName.Contains("italian"))
                {
                    sdbName = "italian_StrDB.sdb";
                }
                else if (table.TableName.Contains("spanish"))
                {
                    sdbName = "spanish_StrDB.sdb";
                }
                else if (table.TableName.Contains("british"))
                {
                    sdbName = "british_StrDB.sdb";
                }
                else if (table.TableName.Contains("korean"))
                {
                    sdbName = "korean_StrDB.sdb";
                }
                else if (table.TableName.Contains("big5"))
                {
                    sdbName = "big5_StrDB.sdb";
                }
                else
                {
                    throw new Exception($"Invalid locale name for table {table.TableName}");
                }

                return sdbName;
            }
            else
            {
                return "UnistrDB.sdb";
            }
        }
    }
}
