using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using CommandLine;
using CommandLine.Text;

using PDTools.SpecDB.Core;

namespace GTSpecDB.Sqlite
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine($"-- GTSpecDB.Sqlite - (c) Nenkai#9075");
            Console.WriteLine();

            var p = Parser.Default.ParseArguments<ExportVerbs, ImportVerbs>(args);

            p.WithParsed<ExportVerbs>(Export)
             .WithParsed<ImportVerbs>(Import)
             .WithNotParsed(HandleNotParsedArgs);

        }

        public static void Export(ExportVerbs exportVerbs)
        {
            if (string.IsNullOrEmpty(exportVerbs.InputPath) || !Directory.Exists(exportVerbs.InputPath))
            {
                Console.WriteLine("Provided input directory does not exist.");
                return;
            }

            string specdbDirName = Path.GetFileNameWithoutExtension(exportVerbs.InputPath);
            SpecDBFolder? type = SpecDB.DetectSpecDBType(specdbDirName);
            if (type is null)
            {
                Console.WriteLine("Unsupported SpecDB Type. Make sure the SpecDB folder has a proper name, example: 'GT4_PREMIUM_US2560'.");
                return;
            }

            if (string.IsNullOrEmpty(exportVerbs.OutputPath))
            {
                string path = Path.GetDirectoryName(exportVerbs.InputPath);
                exportVerbs.OutputPath = Path.Combine(path, specdbDirName) + ".sqlite";
            }

            var db = SpecDB.LoadFromSpecDBFolder(exportVerbs.InputPath, type.Value, false);
            SQLiteExporter exporter = new SQLiteExporter(db);
            exporter.ExportToSQLite(exportVerbs.OutputPath);
        }

        public static void Import(ImportVerbs importVerbs)
        {
            if (!File.Exists(importVerbs.InputPath))
            {
                Console.WriteLine("Provided input file does not exist.");
                return;
            }

            SQLiteImporter importer = new SQLiteImporter();
            importer.Import(importVerbs.InputPath, importVerbs.OutputPath);
        }

        public static void HandleNotParsedArgs(IEnumerable<Error> errors)
        {

        }
    }

    [Verb("export", HelpText = "Export SpecDB to a SQLite file.")]
    public class ExportVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input SpecDB Folder. Example: 'GT4_PREMIUM_US2560'")]
        public string InputPath { get; set; }

        [Option('o', "output", HelpText = "Output sqlite file.")]
        public string OutputPath { get; set; }
    }

    [Verb("import", HelpText = "Imports SQLite to a SpecDB files.")]
    public class ImportVerbs
    {
        [Option('i', "input", Required = true, HelpText = "Input SQLite file. Example: 'GT4_PREMIUM_US2560.sqlite'")]
        public string InputPath { get; set; }
        [Option('o', "output", Required = true, HelpText = "Output SpecDB Folder")]
        public string OutputPath { get; set; }
    }
}
