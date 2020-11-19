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
    public class Supercharger : TableMetadata
    {
        public override string LabelPrefix { get; } = "sc_";

        public Supercharger(string specdbName)
        {
            Columns.Add(new ColumnMetadata("torquemodifier", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("torquemodifier2", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("torquemodifier3", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Unk (UseCar?)", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
        }
    }
}
