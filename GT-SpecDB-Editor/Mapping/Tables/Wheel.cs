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
    public class Wheel : TableMetadata
    {
        public Wheel(string specdbName)
        {
            Columns.Add(new ColumnMetadata("ModelCode", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.UInt));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));

            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));

            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));

            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Byte));
        }
    }
}
