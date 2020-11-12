using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using Syroot.BinaryData.Memory;
using Syroot.BinaryData.Core;

using GT_SpecDB_Editor.Mapping.Types;

namespace GT_SpecDB_Editor.Mapping
{
    [DebuggerDisplay("{Columns.Count} Columns")]
    public abstract class TableMetadata
    {
        public List<ColumnMetadata> Columns { get; set; } = new List<ColumnMetadata>();

        public SpecDBRowData ReadRow(Span<byte> rowData, Endian endian)
        {
            var sr = new SpanReader(rowData, endian);
            var row = new SpecDBRowData();
            foreach (var columnMeta in Columns)
            {
                switch (columnMeta.ColumnType)
                {
                    case DBColumnType.Bool:
                        row.ColumnData.Add(new DBBool(sr.ReadBoolean())); break;
                    case DBColumnType.Byte:
                        row.ColumnData.Add(new DBByte(sr.ReadByte())); break;
                    case DBColumnType.Short:
                        row.ColumnData.Add(new DBShort(sr.ReadInt16())); break;
                    case DBColumnType.Int:
                        row.ColumnData.Add(new DBInt(sr.ReadInt32())); break;
                    case DBColumnType.String:
                        row.ColumnData.Add(new DBString(sr.ReadInt32(), columnMeta.StringFileName)); break;
                    default:
                        break;
                }
            }

            return row;
        }

        public int GetColumnSize()
        {
            int length = 0;
            foreach (var column in Columns)
            {
                switch (column.ColumnType)
                {
                    case DBColumnType.Bool:
                    case DBColumnType.Byte:
                        length++; break;
                    case DBColumnType.Short:
                        length += 2; break;
                    case DBColumnType.Int:
                        length += 4; break;
                    case DBColumnType.String:
                        length += 4; break;
                }
            }
            return length;
        }
    }

    public class ColumnMetadata
    {
        public string ColumnName { get; set; }
        public DBColumnType ColumnType { get; set; }
        public string StringFileName { get; set; }

        public ColumnMetadata(string columnName, DBColumnType columnType)
        {
            ColumnName = columnName;
            ColumnType = columnType;
        }

        public ColumnMetadata(string columnName, DBColumnType columnType, string stringFileName)
        {
            ColumnName = columnName;
            ColumnType = columnType;
            StringFileName = stringFileName;
        }
    }

    public enum DBColumnType
    {
        Bool,
        Byte,
        Short,
        Int,
        String,
        Long,
        Float,
    }
}
