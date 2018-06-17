using System;
using System.Data;
using System.Data.Odbc;
using System.Collections.Generic;
using System.Configuration;
using Mono.Options;

namespace GetColumnNames
{
    class ProgramConfigFile
    {
        private static string _pgmName = null;
        private static int _verbosity = 0;

        private static string _driver = null;
        private static string _server = null;
        private static string _database = null;
        private static string _table = null;
        private static string _options = null;
        private static string _query = null;

        static void Main(string[] args)
        {
            _pgmName = System.AppDomain.CurrentDomain.FriendlyName;  // alternative: System.Diagnostics.Process.GetCurrentProcess().ProcessName

            // Read configuration file
            _driver = ConfigurationManager.AppSettings["Driver"];
            _server = ConfigurationManager.AppSettings["Server"];
            _database = ConfigurationManager.AppSettings["Database"];
            _table = ConfigurationManager.AppSettings["Table"];
            _options = ConfigurationManager.AppSettings["Options"];
            _query = ConfigurationManager.AppSettings["Query"];

            // Parse command line
            bool show_help = false;

            OptionSet p = new OptionSet()
                .Add(
                    "s:|server|S|Server",
                    "The SQL Server instance to connect to.",
                    v => { if (v != null) _server = v; }
                 )
                .Add(
                    "d=|database|D|Database",
                    "The SQL Server database to connect to.",
                    v => { if (v != null) _database = v; }
                 )
                .Add(
                    "t=|table|T|Table",
                    "The SQL Server table whose columns will be returned.",
                    v => { if (v != null) _table = v; }
                 )
                .Add(
                    "v",
                    "Increase debug message verbosity.",
                    v => { if (v != null) ++_verbosity; }
                 )
                .Add(
                    "?|h|help", 
                    "Display this help message.",
                    v => show_help = (v != null)
                 );

            List<string> extra;
            try
            {
                extra = p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write(String.Format("{0}: ", _pgmName));
                Console.WriteLine(e.Message);
                Console.WriteLine(String.Format("Try '{0} --help' for more information.", _pgmName));
                return;
            }

            if (show_help || 
               (_driver == null || _server == null || _database == null || _table == null || _query == null))
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
                        using (OdbcDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                        {
                            for (int col = 0; col < reader.FieldCount; col++)
                            {
                                columns.Add(reader.GetName(col).ToString());  // Gets the column name
                            }

                            reader.Close();
                        }
                    }

                    connection.Close();
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
            Console.WriteLine(String.Format("Usage: {0} -Server VALUE -Database VALUE -Table VALUE\n", _pgmName));
            p.WriteOptionDescriptions(Console.Out);
        }

        private static string GetConnectionString()
        {
            // To avoid storing the connection string in your code,  
            // you can retrieve it from a configuration file.
            return String.Format("Driver={{{0}}};Server={1};Database={2};{3};", _driver, _server, _database, _options);
        }

        private static string GetQueryString()
        {
            return String.Format(_query, _table);
        }

        private static void PrintColumnNames(List<string> columns)
        {
            foreach (string column in columns)
            {
                Console.WriteLine(column);
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
    }
}
