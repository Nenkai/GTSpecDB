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
    public class RacingModify : TableMetadata
    {
        public override string LabelPrefix { get; } = "rm_";

        public RacingModify(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("HasRM", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("GenericCarID", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
        }
    }
}
