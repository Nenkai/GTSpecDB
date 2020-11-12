using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

using GT_SpecDB_Editor.Mapping.Types;

namespace GT_SpecDB_Editor.Mapping
{
    [DebuggerDisplay("{ColumnData.Count} Column Data")]
    public class SpecDBRowData
    {
        public int ID { get; set; }
        public string Label { get; set; }
        public List<IDBType> ColumnData { get; set; } = new List<IDBType>();
    }
}
