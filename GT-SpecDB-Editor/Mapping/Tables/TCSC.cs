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
    public class TCSC : TableMetadata
    {
        public override string LabelPrefix { get; } = "tcsc_";

        public TCSC(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCSparamA", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCSparamB", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCSgrad", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCStarget", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCSUserValueDF", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCSUserValueLevel", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCSUserValueMin", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("TCSUserValueMax", DBColumnType.Byte));

        }
    }
}
