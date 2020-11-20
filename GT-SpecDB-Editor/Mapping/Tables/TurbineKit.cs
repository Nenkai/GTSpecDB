﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Syroot.BinaryData.Core;
using Syroot.BinaryData.Memory;

using GT_SpecDB_Editor.Core;
namespace GT_SpecDB_Editor.Mapping.Tables
{
    public class TurbineKit : TableMetadata
    {
        public override string LabelPrefix { get; } = "tk_";

        public TurbineKit(SpecDBFolder folderType)
        {
            Columns.Add(new ColumnMetadata("Unk", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("torquemodifier", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("torquemodifier2", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("torquemodifier3", DBColumnType.Short));
            Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("wastegate", DBColumnType.Byte));

            Columns.Add(new ColumnMetadata("boost1", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("peakrpm1", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("response1", DBColumnType.Byte));

            Columns.Add(new ColumnMetadata("boost2", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("peakrpm2", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("response2", DBColumnType.Byte));

            Columns.Add(new ColumnMetadata("shiftlimit", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("revlimit", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("rpmeffect", DBColumnType.Byte));
        }
    }
}
