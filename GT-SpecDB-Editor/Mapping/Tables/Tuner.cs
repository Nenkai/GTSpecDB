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
    public class Tuner : TableMetadata
    {
        public Tuner(string specdbName)
        {
            Columns.Add(new ColumnMetadata("Name", DBColumnType.String, "UnistrDB.sdb"));
        }
    }
}
