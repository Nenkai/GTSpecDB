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
    public class GenericCar : TableMetadata
    {
        public GenericCar(string specdbName)
        {
            Columns.Add(new ColumnMetadata("Unk1", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("DefaultParts", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Price", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("SpecifyFlags1", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("PurchaseLevel", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("HornID", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("NumColor", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("MainColor", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Year", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("PowerMax", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("PowerMin", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("Country", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("Maker", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("Tuner", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("Category", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("GenericFlags", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("GenericFlags2", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("GenericFlags3", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("GenericFlags4", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("ConceptCarType", DBColumnType.Byte));
        }
    }
}
