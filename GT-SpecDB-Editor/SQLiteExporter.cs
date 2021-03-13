using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;
using Microsoft.Win32;

using System.Data.SQLite;

using GT_SpecDB_Editor.Core;
using GT_SpecDB_Editor.Mapping;
using GT_SpecDB_Editor.Mapping.Types;

namespace GT_SpecDB_Editor
{
    public class SQLiteExporter
    {
        public SpecDB Database { get; set; }
        public string DBName { get; set; }
        private IProgress<(int, string)> _progress; 

        public SQLiteExporter(SpecDB db, string dbName, IProgress<(int, string)> progress)
        {
            Database = db;
            DBName = dbName;
            _progress = progress;
        }

        public async Task<bool> ExportToSQLiteAsync(ProgressWindow window, string outputFile)
        {
            bool result = false;
            try
            {
                await Task.Run(() => ExportToSQLite(outputFile));
                result = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not save to SQLite Info: {ex.Message}", "Failed to save parts info", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            window.Close();
            return result;
        }

        public void ExportToSQLite(string outputFile)
        {
            SQLiteConnection.CreateFile(outputFile);

            using (var m_dbConnection = new SQLiteConnection($"Data Source={outputFile};Version=3;"))
            {
                m_dbConnection.Open();

                _progress.Report((0, "Creating Tables.."));
                CreateTables(m_dbConnection);

                _progress.Report((0, "Creating Table Info.."));
                CreateTableInfo(m_dbConnection);

                _progress.Report((0, "Filling Table Info.."));
                InsertTableInfo(m_dbConnection);

                InsertTableRows(m_dbConnection);
            }
        }

        private void CreateTables(SQLiteConnection conn)
        {
            foreach (var table in Database.Tables.Values)
            {
                if (!table.IsLoaded)
                    table.LoadAllRows(Database);

                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {table.TableName} (RowId int, Label varchar(64), ");

                for (int i = 0; i < table.TableMetadata.Columns.Count; i++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[i];

                    int sameNameCount = 0;
                    string columnName = col.ColumnName;
                    if (columnName == "?")
                        columnName = "Unk";

                    for (int j = 0; j < i; j++)
                    {
                        if (table.TableMetadata.Columns[j].ColumnName.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase))
                            sameNameCount++;
                    }

                    if (sameNameCount > 0)
                        columnName += $"_{sameNameCount}";
                    sb.Append(columnName);
                    sb.Append($" {TranslateSpecDBTypeToSQLite(col)}");

                    if (i < table.TableMetadata.Columns.Count - 1)
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
                if (!table.IsLoaded)
                    table.LoadAllRows(Database);

                StringBuilder sb = new StringBuilder();
                sb.Append($"CREATE TABLE {table.TableName}_typeinfo (");

                for (int i = 0; i < table.TableMetadata.Columns.Count; i++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[i];

                    int sameNameCount = 0;
                    string columnName = col.ColumnName;
                    if (columnName == "?")
                        columnName = "Unk";

                    for (int j = 0; j < i; j++)
                    {
                        if (table.TableMetadata.Columns[j].ColumnName.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase))
                            sameNameCount++;
                    }

                    if (sameNameCount > 0)
                        columnName += $"_{sameNameCount}";
                    sb.Append(columnName);
                    sb.Append($" varchar(64)");

                    if (i < table.TableMetadata.Columns.Count - 1)
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
                if (!table.IsLoaded)
                    table.LoadAllRows(Database);

                StringBuilder sb = new StringBuilder();
                sb.Append($"INSERT INTO {table.TableName}_typeinfo (");

                for (int i = 0; i < table.TableMetadata.Columns.Count; i++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[i];

                    int sameNameCount = 0;
                    string columnName = col.ColumnName;
                    if (columnName == "?")
                        columnName = "Unk";

                    for (int j = 0; j < i; j++)
                    {
                        if (table.TableMetadata.Columns[j].ColumnName.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase))
                            sameNameCount++;
                    }

                    if (sameNameCount > 0)
                        columnName += $"_{sameNameCount}";
                    sb.Append(columnName);
                    if (i < table.TableMetadata.Columns.Count - 1)
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
                SQLiteCommand command = new SQLiteCommand(sb.ToString(), conn);
                command.ExecuteNonQuery();
            }
        }

        private void InsertTableRows(SQLiteConnection conn)
        {
            List<SpecDBTable> list = Database.Tables.Values.ToList();
            for (int i1 = 0; i1 < list.Count; i1++)
            {
                SpecDBTable table = list[i1];
                if (!table.IsLoaded)
                    table.LoadAllRows(Database);

                _progress.Report((i1, $"Inserting Rows in {table.TableName}."));

                StringBuilder sb = new StringBuilder();
                sb.Append($"INSERT INTO {table.TableName} (RowId, Label, ");

                for (int i = 0; i < table.TableMetadata.Columns.Count; i++)
                {
                    ColumnMetadata col = table.TableMetadata.Columns[i];

                    int sameNameCount = 0;
                    string columnName = col.ColumnName;
                    if (columnName == "?")
                        columnName = "Unk";

                    for (int j = 0; j < i; j++)
                    {
                        if (table.TableMetadata.Columns[j].ColumnName.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase))
                            sameNameCount++;
                    }

                    if (sameNameCount > 0)
                        columnName += $"_{sameNameCount}";
                    sb.Append(columnName);
                    if (i < table.TableMetadata.Columns.Count - 1)
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
                    return "varchar(128)";
                case DBColumnType.Float:
                    return "real";
                default:
                    return "null";
            }
        }
    }
}
