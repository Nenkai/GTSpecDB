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
    public class EnemyCars : TableMetadata
    {
        public EnemyCars(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("GenericCar", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Gen.Tbl.Index", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("DefaultParts", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("DefPrs.Tbl.Index", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("DefaultParam", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("DefPrm.Tbl.Index", DBColumnType.Int));
        }
    }
}
