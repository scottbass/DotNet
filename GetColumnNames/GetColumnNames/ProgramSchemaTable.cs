using System;
using System.Data;
using System.Data.Odbc;
using System.Collections.Generic;
using Mono.Options;

namespace GetColumnNames
{
    class ProgramSchemaTable
    {
        private static int _verbosity;
        private static string _server = null;
        private static string _database = null;
        private static string _table = null;
        private const string _options = @"Trusted_Connection=Yes";  // Any additional connection options

        static void Main(string[] args)
        {
            // Parse command line
            bool show_help = false;

            OptionSet p = new OptionSet()
                .Add(
                    "s=|server=|S=|Server=", 
                    "The SQL Server instance to connect to.",
                    v => _server = v
                 )
                .Add(
                    "d=|database=|D=|Database=",
                    "The SQL Server database to connect to.",
                    v => _database = v
                 )
                .Add(
                    "t=|table=|T=|Table=",
                    "The SQL Server table whose columns will be returned.",
                    v => _table = v
                 )
                .Add(
                    "v",
                    "Increase debug message verbosity.",
                    v => { if (v != null) ++_verbosity; }
                 )
                .Add(
                    "?|h|help", 
                    "Display this help message.",
                    v => show_help = v != null
                 );

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("GetColumnNames: ");
                Console.WriteLine(e.Message);
                Console.WriteLine("Try 'GetColumnNames --help' for more information.");
                return;
            }

            if (show_help || (_server == null || _database == null || _table == null))
            {
                ShowHelp(p);
                return;
            }

            // Main processing
            List<string> columns = new List<string>();
            try
            {
                using (OdbcConnection connection = new OdbcConnection(GetConnectionString()))
                {
                    // Connect to the database.
                    Debug("Connection String: {0}", connection.ConnectionString);
                    connection.Open();

                    using (OdbcCommand cmd = new OdbcCommand(GetQueryString(), connection))
                    {
                        Debug("Query String: {0}", cmd.CommandText);
                        using (OdbcDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo))  // must use KeyInfo, not SchemaOnly
                        {
                            // Retrieve column schema into a DataTable.
                            DataTable schemaTable = reader.GetSchemaTable();  // similar to INFORMATION_SCHEMA.columns

                            // For each row in the table...
                            foreach (DataRow row in schemaTable.Rows)
                            {
                                // Load the list with the column names
                                columns.Add(row["ColumnName"].ToString());
                            }

                            // Print debug information
                            DebugFields(schemaTable);
                        }
                    }
                }
                PrintColumnNames(columns);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void ShowHelp(OptionSet p)
        {
            Console.WriteLine("Usage: GetColumnNames.exe -Server VALUE -Database VALUE -Table VALUE\n");
            p.WriteOptionDescriptions(Console.Out);
        }

        private static string GetConnectionString()
        {
            // To avoid storing the connection string in your code,  
            // you can retrieve it from a configuration file.
            return String.Format("Driver={{SQL Server Native Client 11.0}};Server={0};Database={1};{2};", _server, _database, _options);
        }

        private static string GetQueryString()
        {
            return String.Format("SELECT * FROM {0}", _table);
        }

        private static void PrintColumnNames(List<string> columns)
        {
            foreach (string cols in columns)
            {
                Console.WriteLine(cols);
            }
        }

        static void Debug(string format, params object[] args)
        {
            if (_verbosity > 0)
            {
                Console.Write("# ");
                Console.WriteLine(format, args);
            }
        }

        static void DebugFields(DataTable schemaTable)
        {
            if (_verbosity > 0)
            {
                // For each row in the table...
                foreach (DataRow field in schemaTable.Rows)
                {
                    // For each column in the row...
                    foreach (DataColumn property in schemaTable.Columns)
                    {
                        // Display the field name and value.
                        Console.Write("# ");
                        Console.WriteLine(String.Format("{0,-32}: {1}", property, field[property].ToString()));
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
