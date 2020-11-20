using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GT_SpecDB_Editor.Core;
namespace GT_SpecDB_Editor.Mapping.Tables
{
    public class Variation : TableMetadata
    {
        public Variation(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("VarOrder", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("NameJpn", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("NameEng", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("Flag", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("ColorChip0", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("ColorChip1", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("ColorChip2", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("ColorChip3", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("CarColorID", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("AllPaintID", DBColumnType.UInt));
        }
    }
}
