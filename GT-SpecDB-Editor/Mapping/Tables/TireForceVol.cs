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
    public class TireForceVol : TableMetadata
    {
        public TireForceVol(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("Placeholder Table", DBColumnType.Byte));
        }
    }
}
