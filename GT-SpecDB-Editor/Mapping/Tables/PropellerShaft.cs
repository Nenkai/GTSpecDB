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
    public class PropellerShaft : TableMetadata
    {
        public override string LabelPrefix { get; } = "ps_";

        public PropellerShaft(string specdbName)
        {
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("enginebrake", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("iwheelF", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("iwheelR", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("ipropF", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("ipropR", DBColumnType.Byte));
        }
    }
}
