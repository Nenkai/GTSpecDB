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
    public class CarCustomInfo : TableMetadata
    {
        public CarCustomInfo(string specdbName)
        {
            Columns.Add(new ColumnMetadata("BitsA", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("BitsB", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("BitsC", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("BitsD", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("BitsE", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("BitsF", DBColumnType.Int));
        }
    }
}
