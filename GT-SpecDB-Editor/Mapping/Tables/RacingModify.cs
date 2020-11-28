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
    public class RacingModify : TableMetadata
    {
        public override string LabelPrefix { get; } = "rm_";

        public RacingModify(SpecDBFolder folderType)
        {
            if (folderType >= SpecDBFolder.GT5_JP3009)
            {
                Columns.Add(new ColumnMetadata("HasRM", DBColumnType.Int));
                Columns.Add(new ColumnMetadata("NewCarCode", DBColumnType.Int));
                Columns.Add(new ColumnMetadata("category", DBColumnType.Byte));
            }
            else
            {
                // Chassis data, GT5 retail moved them to CHASSIS
                Columns.Add(new ColumnMetadata("Unk", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("chassisTreadF", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("chassisThreadR", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("chassisWidth", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("chassisDWidth", DBColumnType.Short));
                
                Columns.Add(new ColumnMetadata("HasData", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Unk", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("cd", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("clMINF", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("clMAXF", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("clDFF", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("clMINR", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("clMAXR", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("clDFR", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("wheeloffsetF", DBColumnType.SByte));
                Columns.Add(new ColumnMetadata("wheeloffsetR", DBColumnType.SByte));
                Columns.Add(new ColumnMetadata("susarmF", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("susarmR", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("UnkB", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("HasActiveWing", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("ActiveWingType", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("ActiveWingVelocity1", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("ActiveWingVelocity2", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("ActiveWingMovingSpeed", DBColumnType.Byte));
            }
        }
    }
}
