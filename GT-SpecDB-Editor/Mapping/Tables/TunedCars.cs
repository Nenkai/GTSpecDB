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
    public class TunedCars : TableMetadata
    {
        public TunedCars(string specdbName)
        {
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("CarCode", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Def. Parts Table Index", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("ID", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Def. Param Table Index", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("ID", DBColumnType.Int));

        }
    }
}
