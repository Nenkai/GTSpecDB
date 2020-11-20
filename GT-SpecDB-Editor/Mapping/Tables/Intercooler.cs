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
    public class Intercooler : TableMetadata
    {
        public override string LabelPrefix { get; } = "ic_";

        public Intercooler(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("torquemodifier", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("torquemodifier2", DBColumnType.Short));

            if (folderType >= SpecDBFolder.GT5_JP3009)
                Columns.Add(new ColumnMetadata("torquemodifier3", DBColumnType.Short));

            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
        }
    }
}
