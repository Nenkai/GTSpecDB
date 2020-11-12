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
    public class FrontTire : TableMetadata
    {
        public FrontTire(string specdbName)
        {
            Columns.Add(new ColumnMetadata("Unk1", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("tiresize", DBColumnType.Int));

            Columns.Add(new ColumnMetadata("Unk2", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("tirecompound0", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Unk3", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("tirecompound1", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Unk4", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("tirecompound2", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Unk5", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("tireforcevol0", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Unk6", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("tireforcevol1", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Unk7", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("tireforcevol2", DBColumnType.Int));

            Columns.Add(new ColumnMetadata("Price", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("tireDrainageLevel", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("tireSpring_Auto", DBColumnType.Byte));
        }
    }
}
