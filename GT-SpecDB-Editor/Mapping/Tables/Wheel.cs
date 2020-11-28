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
    public class Wheel : TableMetadata
    {
        public Wheel(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("ModelCode", DBColumnType.UInt));
            if (folderType >= SpecDBFolder.GT5_JP3009)
            {
                Columns.Add(new ColumnMetadata("ThumbnailID", DBColumnType.UInt));
                Columns.Add(new ColumnMetadata("VarOrder", DBColumnType.Short));
            }
            else
            {
                Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            }

            Columns.Add(new ColumnMetadata("FrontTireOffset", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("FrontDiameter", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("FrontAspect", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("FrontWidth", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("FrontTireID", DBColumnType.Short));

            Columns.Add(new ColumnMetadata("RearTireOffset", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("RearDiameter", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("RearAspect", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("RearWidth", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("RearTireID", DBColumnType.Short));

            if (folderType >= SpecDBFolder.GT5_JP3009)
            {
                Columns.Add(new ColumnMetadata("WheelType", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("WheelNumColor", DBColumnType.Byte));
            }
        }
    }
}
