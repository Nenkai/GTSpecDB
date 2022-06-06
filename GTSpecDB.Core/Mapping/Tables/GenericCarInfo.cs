using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GTSpecDB.Core;
namespace GTSpecDB.Mapping.Tables
{
    public class GenericCarInfo : TableMetadata
    {
        public GenericCarInfo(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));

            if (folderType == SpecDBFolder.GT5_JP3003)
            {
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
            }
        }
    }
}
