using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using GTSpecDB.Core;

namespace GTSpecDB.Sqlite
{
    class Program
    {

        static async Task Main(string[] args)
        {
            Console.WriteLine($"-- GTSpecDBSqlite - (c) Nenkai#9075, ported from flatz's gttool --");
            Console.WriteLine();

            
            var db = SpecDB.LoadFromSpecDBFolder(@"D:\Modding_Research\Gran_Turismo\Gran_Turismo_4_Online\EXTRACTED\specdb\GT4_PREMIUM_US2560", SpecDBFolder.GT4_PREMIUM_US2560, false);
            SQLiteExporter exporter = new SQLiteExporter(db);
            exporter.ExportToSQLite("GT4_PREMIUM_US2560.sqlite");

            SQLiteImporter importer = new SQLiteImporter();
            importer.Import("GT4_PREMIUM_US2560.sqlite", "Export");
        }
    }
}
