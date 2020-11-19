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
    public class Lightweight : TableMetadata
    {
        public override string LabelPrefix { get; } = "lw_";

        public Lightweight(string specdbName)
        {
            Columns.Add(new ColumnMetadata("weighteffect", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk (UseCar?)", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("yaweffect", DBColumnType.Byte));
        }
    }
}
