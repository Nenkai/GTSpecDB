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
    public class Brake : TableMetadata
    {
        public override string LabelPrefix { get; } = "br_";
        public Brake(string specdbName)
        {
            Columns.Add(new ColumnMetadata("Price", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Category", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("BraketorqueF", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("BraketorqueR", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("Sidebraketorque", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("tireMuForBrake", DBColumnType.Byte));
        }
    }
}
