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
    public class CarName : TableMetadata
    {
        public override string LabelPrefix { get; } = "";

        public CarName(string specdbName, string localeName)
        {
            Columns.Add(new ColumnMetadata("Name", DBColumnType.String, $"{localeName}_StrDB.sdb"));
            Columns.Add(new ColumnMetadata("Grade", DBColumnType.String, $"{localeName}_StrDB.sdb"));
            Columns.Add(new ColumnMetadata("ShortName", DBColumnType.String, $"{localeName}_StrDB.sdb"));
        }
    }
}
