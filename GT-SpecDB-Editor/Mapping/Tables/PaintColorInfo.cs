using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GT_SpecDB_Editor.Mapping.Types;
namespace GT_SpecDB_Editor.Mapping.Tables
{
    public class PaintColorInfo : TableMetadata
    {
        public PaintColorInfo(string specdbName)
        {
            Columns.Add(new ColumnMetadata("LabelEng", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("LabelJpn", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("ColorChip", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("ColorChip2", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("CCBinID", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk1", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("Type", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("TunerID", DBColumnType.Short));
        }
    }
}
