using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;
using System.IO;

using System.Data.SQLite;

using GTSpecDB.Mapping;
using GTSpecDB.Mapping.Types;

namespace GTSpecDB.Core
{
    public class SQLiteExporter
    {
        public SpecDB Database { get; set; }

        private HashSet<SpecDBTable> _errornousTables = new HashSet<SpecDBTable>();

        public SQLiteExporter(SpecDB db)
        {
            Database = db;
        }

        public void ExportToSQLite(string outputFile)
        {
            if (File.Exists(outputFile))
                File.Delete(outputFile);

            Console.WriteLine("Loading SpecDB Tables...");

            foreach (var table in Database.Tables.Values)
            {
                try
                {
                    Console.WriteLine($"Loading Table - {table.TableName}");
                    if (!table.IsLoaded)
                        table.LoadAllRows(Database);
                }
                catch (Exception e)
                {
                    _errornousTables.Add(table);
                }
            }

            if (_errornousTables.Count > 0)
            {
                string tables = string.Join("\n", _errornousTables.Select(t => $"- {t.TableName}"));
                Console.WriteLine($"The following tables can not be loaded/unmapped:\n{tables}\n\n Continue? [y/n]");

                if (Console.ReadKey().Key != ConsoleKey.Y)
                    return;
            }

            SQLiteConnection.CreateFile(outputFile);

            using (var m_dbConnection = new SQLiteConnection($"Data Source={outputFile};Version=3;"))
            {
                m_dbConnection.Open();

                Console.WriteLine("Creating Tables in SQLite..");
                CreateTables(m_dbConnection);

                Console.WriteLine("Creating Table Info..");
                CreateTableInfo(m_dbConnection);

                Console.WriteLine("Filling Table Info..");
                InsertTableInfo(m_dbConnection);

                InsertTableRows(m_dbConnection);

                InsertDatabaseInfo(m_dbConnection);
            }
        }

        private void CreateTables(SQLiteConnection conn)
        {
            foreach (var table in Database.Tables.Values)
            {
                if (_errornousTables.Contains(table))
                    continue;

                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {table.TableName} (RowId int, Label Text, ");

                for (int colIndex = 0; colIndex < table.TableMetadata.Columns.Count; colIndex++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[colIndex];
                    string columnName = GetValidColumnName(table, colIndex, col);

                    sb.Append(columnName);
                    sb.Append($" {TranslateSpecDBTypeToSQLite(col)}");

                    if (colIndex < table.TableMetadata.Columns.Count - 1)
                        sb.Append(", ");
                }

                sb.Append(')');
                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.ExecuteNonQuery();
            }
        }

        private void CreateTableInfo(SQLiteConnection conn)
        {
            foreach (var table in Database.Tables.Values)
            {
                if (_errornousTables.Contains(table))
                    continue;

                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {table.TableName}_typeinfo (");

                for (int colIndex = 0; colIndex < table.TableMetadata.Columns.Count; colIndex++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[colIndex];
                    string columnName = GetValidColumnName(table, colIndex, col);

                    sb.Append(columnName);
                    sb.Append($" TEXT");

                    if (colIndex < table.TableMetadata.Columns.Count - 1)
                        sb.Append(", ");
                }

                sb.Append(')');
                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.ExecuteNonQuery();
            }
        }

        private void InsertTableInfo(SQLiteConnection conn)
        {
            foreach (var table in Database.Tables.Values)
            {
                if (_errornousTables.Contains(table))
                    continue;

                // Add column names
                StringBuilder sb = new StringBuilder();
                sb.Append($"INSERT INTO {table.TableName}_typeinfo (");
                for (int colIndex = 0; colIndex < table.TableMetadata.Columns.Count; colIndex++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[colIndex];
                    string columnName = GetValidColumnName(table, colIndex, col);
                    sb.Append(columnName);
                    if (colIndex < table.TableMetadata.Columns.Count - 1)
                        sb.Append(", ");
                }

                sb.Append(") values (");
                for (int colIndex = 0; colIndex < table.TableMetadata.Columns.Count; colIndex++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[colIndex];
                    string columnName = GetValidColumnName(table, colIndex, col);
                    sb.Append($"'{columnName}'");
                    if (colIndex < table.TableMetadata.Columns.Count - 1)
                        sb.Append(", ");
                }
                sb.Append(')');

                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.ExecuteNonQuery();

                // Add column types
                sb.Clear();
                sb.Append($"INSERT INTO {table.TableName}_typeinfo (");
                for (int colIndex = 0; colIndex < table.TableMetadata.Columns.Count; colIndex++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[colIndex];
                    string columnName = GetValidColumnName(table, colIndex, col);
                    sb.Append(columnName);
                    if (colIndex < table.TableMetadata.Columns.Count - 1)
                        sb.Append(", ");
                }

                sb.Append(") values (");
                for (int i = 0; i < table.TableMetadata.Columns.Count; i++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[i];
                    sb.Append($"'{col.ColumnType}'");
                    if (i < table.TableMetadata.Columns.Count - 1)
                        sb.Append(", ");
                }
                sb.Append(')');
                command = new SQLiteCommand(sb.ToString(), conn);
                command.ExecuteNonQuery();
            }
        }

        private static string GetValidColumnName(SpecDBTable table, int colIndex, ColumnMetadata col)
        {
            int sameNameCount = 0;
            string columnName = col.ColumnName;
            if (columnName == "?")
                columnName = "Unk";

            for (int j = 0; j < colIndex; j++)
            {
                if (table.TableMetadata.Columns[j].ColumnName.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase))
                    sameNameCount++;
            }

            if (sameNameCount > 0)
                columnName += $"_{sameNameCount}";
            return columnName;
        }

        private void InsertTableRows(SQLiteConnection conn)
        {
            List<SpecDBTable> list = Database.Tables.Values.ToList();
            for (int i1 = 0; i1 < list.Count; i1++)
            {
                SpecDBTable table = list[i1];
                if (_errornousTables.Contains(table))
                    continue;

                Console.WriteLine($"Inserting Rows in {table.TableName}.");

                StringBuilder sb = new StringBuilder();
                sb.Append($"INSERT INTO {table.TableName} (RowId, Label, ");

                for (int colIndex = 0; colIndex < table.TableMetadata.Columns.Count; colIndex++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[colIndex];
                    string columnName = GetValidColumnName(table, colIndex, col);
                    sb.Append(columnName);
                    if (colIndex < table.TableMetadata.Columns.Count - 1)
                        sb.Append(", ");
                }

                sb.Append(") values (");

                string preInsertQuery = sb.ToString();

                using (var transac = conn.BeginTransaction())
                {
                    for (int i = 0; i < table.Rows.Count; i++)
                    {
                        sb.Clear();
                        var row = table.Rows[i];
                        sb.Append($"{row.ID}, '{row.Label}', ");

                        for (int j = 0; j < row.ColumnData.Count; j++)
                        {
                            ColumnMetadata col = table.TableMetadata.Columns[j];

                            switch (col.ColumnType)
                            {
                                case DBColumnType.Bool:
                                    sb.Append((row.ColumnData[j] as DBBool).Value ? 1 : 0); break;
                                case DBColumnType.Byte:
                                    sb.Append((row.ColumnData[j] as DBByte).Value); break;
                                case DBColumnType.Long:
                                    sb.Append((row.ColumnData[j] as DBLong).Value); break;
                                case DBColumnType.Short:
                                    sb.Append((row.ColumnData[j] as DBShort).Value); break;
                                case DBColumnType.SByte:
                                    sb.Append((row.ColumnData[j] as DBSByte).Value); break;
                                case DBColumnType.UInt:
                                    sb.Append((row.ColumnData[j] as DBUInt).Value); break;
                                case DBColumnType.UShort:
                                    sb.Append((row.ColumnData[j] as DBUShort).Value); break;
                                case DBColumnType.Int:
                                    sb.Append((row.ColumnData[j] as DBInt).Value); break;
                                case DBColumnType.String:
                                    sb.Append($"'{(row.ColumnData[j] as DBString).Value.Replace("'", "''")}'"); break;
                                case DBColumnType.Float:
                                    sb.Append((row.ColumnData[j] as DBFloat).Value); break;
                            }
                            
                            if (j < row.ColumnData.Count - 1)
                                sb.Append(", ");
                        }

                        sb.Append(')');
                        SQLiteCommand command = new SQLiteCommand(preInsertQuery + sb.ToString(), conn);
                        command.ExecuteNonQuery();
                    }

                    transac.Commit();
                }
            }
        }

        private void InsertDatabaseInfo(SQLiteConnection conn)
        {
            SQLiteCommand command = new SQLiteCommand($"CREATE TABLE _DatabaseTableInfo (TableName Text, TableID int, BigEndian int)", conn);
            command.ExecuteNonQuery();

            foreach (var table in Database.Tables.Values)
            {
                if (_errornousTables.Contains(table))
                    continue;

                StringBuilder sb = new StringBuilder();

                command = new SQLiteCommand("INSERT INTO _DatabaseTableInfo (TableName, TableId, BigEndian) VALUES (@name, @id, @bigendian)", conn);
                command.Parameters.AddWithValue("@name", table.TableName);
                command.Parameters.AddWithValue("@id", table.TableID);
                command.Parameters.AddWithValue("@bigendian", table.DBT.Endian == Syroot.BinaryData.Core.Endian.Big ? 1 : 0);
                command.ExecuteNonQuery();
            }

            command = new SQLiteCommand($"CREATE TABLE _DatabaseInfo (SpecDBName Text)", conn);
            command.ExecuteNonQuery();

            command = new SQLiteCommand($"INSERT INTO _DatabaseInfo (SpecDBName) VALUES (@name)", conn);
            command.Parameters.AddWithValue("@name", Database.SpecDBFolderType.ToString());
            command.ExecuteNonQuery();
        }

        public static string TranslateSpecDBTypeToSQLite(ColumnMetadata column)
        {
            switch (column.ColumnType)
            {
                case DBColumnType.Bool:
                case DBColumnType.Byte:
                case DBColumnType.Long:
                case DBColumnType.Short:
                case DBColumnType.SByte:
                case DBColumnType.UInt:
                case DBColumnType.UShort:
                case DBColumnType.Int:
                    return "int";
                case DBColumnType.String:
                    return "TEXT";
                case DBColumnType.Float:
                    return "real";
                default:
                    return "null";
            }
        }
    }
}
