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
    public class Course : TableMetadata
    {
        public Course(string specdbName)
        {
            Columns.Add(new ColumnMetadata("ModelName", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("NameJpn", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("NameEng", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("PitCrew", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Condition", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("EntryMax", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("CourseTopology", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("NumberOfLanes", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("HasPitLane", DBColumnType.Bool));
            Columns.Add(new ColumnMetadata("GarageSide", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("StartingGridCount", DBColumnType.Byte));
            Columns.Add(new ColumnMetadata("PitStopCount", DBColumnType.Byte));

            if (specdbName.Equals("GT5_JP3010"))
            {
                
            }
        }
    }
}
