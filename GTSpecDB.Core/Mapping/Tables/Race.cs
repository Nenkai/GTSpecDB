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
    public class Race : TableMetadata
    {
        public Race(SpecDBFolder folderType)
        {
            if (folderType <= SpecDBFolder.GT5_TRIAL_JP2704)
            {
                Columns.Add(new ColumnMetadata("CourseID", DBColumnType.Int));
                Columns.Add(new ColumnMetadata("Crs_Tbl_Index", DBColumnType.Int));
                Columns.Add(new ColumnMetadata("AllowEntry", DBColumnType.Int));
                Columns.Add(new ColumnMetadata("AlEnt_Tbl_Index", DBColumnType.Int));
                Columns.Add(new ColumnMetadata("RaceMode", DBColumnType.String, "UnistrDB.sdb"));
                Columns.Add(new ColumnMetadata("StartV", DBColumnType.Int));
                Columns.Add(new ColumnMetadata("RaceMinutes", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("goldfrac", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("silverfrac", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("bronzefrac", DBColumnType.Short));

                // 20 to 2a shorts -> prize
                Columns.Add(new ColumnMetadata("Prize1", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("Prize2", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("Prize3", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("Prize4", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("Prize5", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("Prize6", DBColumnType.Short));

                Columns.Add(new ColumnMetadata("ChampPrize", DBColumnType.Short));

                // 2e to 36 shorts -> launch pos
                Columns.Add(new ColumnMetadata("LaunchPosition1", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("LaunchPosition2", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("LaunchPosition3", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("LaunchPosition4", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("LaunchPosition5", DBColumnType.Short));

                Columns.Add(new ColumnMetadata("YearMin", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("YearMax", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("UnkFlag", DBColumnType.Short));
                Columns.Add(new ColumnMetadata("MaxEntries", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("StartType", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("unkdrag", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("PlBoost", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("NumberOfLaps", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("FailCond", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("NeedLicense", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("NeedDrivetrain", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("NeedAspiration", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("NeedCarType", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("NeedTyres", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Skill1", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk2", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk3", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk4", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk5", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk6", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk7", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk8", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk9", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk10", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk11", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk12", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("Sk13", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("BoostUnk", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("BoostMaybe", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("BoostMaybe", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("BoostMaybe", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("BoostUnk", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("BoostUnk", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("BoostUnk", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("TireWear1", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear2", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear3", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear4", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear5", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear6", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear7", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear8", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("TireWear9", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("goldmin", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("goldsec", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("silvermin", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("silversec", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("bronzemin", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("bronzesec", DBColumnType.Byte));

                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LaunchSpeed1", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LaunchSpeed2", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LaunchSpeed3", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LaunchSpeed4", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LaunchSpeed5", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LaunchSpeed6", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("?", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("DelayStart1", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("DelayStart2", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("DelayStart3", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("DelayStart4", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("DelayStart5", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("DelayStart6", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LimitPower", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("NeedPower", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LimitWeight", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("LimitLength", DBColumnType.Byte));
                Columns.Add(new ColumnMetadata("NeedCountry", DBColumnType.Byte));
            }
            else
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
}
