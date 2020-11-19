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
    public class Race : TableMetadata
    {
        public Race(string specdbName)
        {
            Columns.Add(new ColumnMetadata("CourseLabel", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("AllowEntry", DBColumnType.String, "UnistrDB.sdb"));
            Columns.Add(new ColumnMetadata("goldfrac", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("silverfrac", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("bronzefrac", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize1", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize2", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize3", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize4", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize5", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize6", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize7", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize8", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize9", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize10", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize11", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize12", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize13", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize14", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize15", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("Prize16", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("prizeGC", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition1", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition2", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition3", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition4", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition5", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition6", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition7", DBColumnType.Int));
            Columns.Add(new ColumnMetadata("LaunchPosition8", DBColumnType.Int));
        }
    }
}
