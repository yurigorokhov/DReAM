﻿/*
 * MindTouch Dream - a distributed REST framework 
 * Copyright (C) 2006-2011 MindTouch, Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace MindTouch.Data.Db {
    class UpdateDb {

        internal class DBConnection {
            public string dbServer;
            public string dbName;
            public string dbUsername;
            public string dbPassword;

            // Parses a string of format : dbName[;dbServer;dbUsername;dbPassword]
            public static DBConnection Parse(string configString, string defaultServer, string defaultUser, string defaultPassword) {
                if(string.IsNullOrEmpty(configString)) {
                    return null;
                }
                var items = configString.Split(';');
                var con = new DBConnection();
                con.dbName = items[0];
                if(items.Length == 0) {
                    con.dbServer = defaultServer;
                    con.dbUsername = defaultUser;
                    con.dbPassword = defaultPassword;
                } else if(items.Length == 4) {
                    con.dbServer = items[1];
                    con.dbUsername = items[2];
                    con.dbPassword = items[3];
                }
                return con;
            }   
        }

        //--- Class Methods ---
        static int Main(string[] args) {
            string dbusername = "root", dbname = "wikidb", dbserver = "localhost", dbpassword = null, updateDLL = null, 
                   targetVersion = null, sourceVersion = null, customMethods = null;
            int dbport = 3306, exit = 0;
            bool showHelp = false, dryrun = false, verbose = false, listDatabases = false;

            // set command line options
            var options = new Options() {
                { "p=|dbpassword=", "Database password", p => dbpassword = p },
                { "v=|version=", "Target Version", v => targetVersion = v },
                { "b=|sversion=", "Source Version", b => sourceVersion = b },
                { "u=|dbusername=", "Database user name (default: root)", u => dbusername = u },
                { "d=|dbname=", "Database name (default: wikidb)", p => dbname = p },
                { "s=|dbserver=", "Database server (default: localhost)", s => dbserver = s },
                { "l=|listdb" , "List of databases separated by EOF", l => listDatabases = true},
                { "n=|port=", "Database port (default: 3306)", n => {dbport = Int32.Parse(n);}},
                { "c=|custom", "Custom Methods to invoke (comma separated list)", c => {customMethods = c;}},
                { "i|info", "Display verbose information (default: false)", i => {verbose = true;}},
                { "f|dryrun", "Just display the methods that will be called, do not execute any of them. (default: false)", f => { dryrun = verbose = true;} },
                { "h|?|help", "show help text", h => { verbose = true; }},
            };
            if(args == null || args.Length == 0) {
                showHelp = true;
            } else {
                try {
                    var trailingOptions = options.Parse(args).ToArray();

                    // if there are more trailing arguments display help
                    if(trailingOptions.Length != 1) {
                        showHelp = true;
                    } else {
                        updateDLL = Path.GetFullPath(trailingOptions.First());
                    }
                } catch(InvalidOperationException) {
                    exit = -3;
                    Console.Error.WriteLine("Invalid arguments");
                    showHelp = true;
                }
            }
            if(!showHelp) {

                // Check Arguments
                CheckArg(updateDLL, "No DLL file was specified");
                if(!string.IsNullOrEmpty(dbname)) {
                    CheckArg(dbpassword, string.Format("No Database password specified for database {0}", dbname));
                }

                // If there are no custom methods specified we need a version number
                if(customMethods == null) {
                    CheckArg(targetVersion, "No version specified");
                }

                // Begin Parsing DLL
                var dllAssembly = Assembly.LoadFile(updateDLL);
                        
                // Read list of databases if listDatabases is true
                var databaseList = new List<DBConnection>();
                if(listDatabases) {
                    
                    // Read the db names from input
                    // format: dbname[;dbserver;dbuser;dbpassword]
                    string line = null;
                    while(!string.IsNullOrEmpty(line = Console.ReadLine())) {
                        var connection = DBConnection.Parse(line, dbserver, dbusername, dbpassword);
                        if(connection != null) {
                            databaseList.Add(connection);
                        }
                    }
                }

                // Instatiate Mysql Upgrade class
                MysqlDataUpdater mysqlSchemaUpdater = null;
                try {
                    mysqlSchemaUpdater = new MysqlDataUpdater(dbserver, dbport, dbname, dbusername, dbpassword, targetVersion);
                    if(sourceVersion != null) {
                        mysqlSchemaUpdater.SourceVersion = sourceVersion;
                    }
                } catch(VersionInfoException) {
                    PrintErrorAndExit("You entered an incorrect version numner.");
                }

                // Run update
                runUpdate(mysqlSchemaUpdater, dllAssembly, customMethods, targetVersion, verbose, dryrun);

            }
            else {
                ShowHelp(options);
            } 
            return exit;
        }

        private static void runUpdate(MysqlDataUpdater site, Assembly dllAssembly, string customMethods,string targetVersion, bool verbose, bool dryrun) {
            // Execute custom methods
            if(customMethods != null) {
                var methods = customMethods.Split(',');
                foreach(var method in methods) {
                    if(verbose) {
                        Console.WriteLine(String.Format("Executing custom method: {0}", method));
                    }
                    if(!dryrun) {
                        site.ExecuteCustomMethod(method.Trim(), dllAssembly);
                    }
                }
            }

            // Execute update methods
            if(targetVersion != null) {
                site.LoadMethods(dllAssembly);
                var methods = site.GetMethods();

                // Execute each method
                foreach(var method in methods) {
                    if(verbose) {
                        Console.WriteLine(String.Format("Executing method: {0}", method));
                    }
                    if(!dryrun) {
                        try { site.TestConnection(); } catch(Exception) {
                            System.Threading.Thread.Sleep(5000);
                            site.TestConnection();
                        }
                        site.ExecuteMethod(method);
                    }
                }
            }
        }

        private static void ShowHelp(Options p) {
            var sw = new StringWriter();
            sw.WriteLine("Usage: mindtouch.db.exe -p password -v version mindtouch.deki.db.dll");
            p.WriteOptionDescriptions(sw);
            Console.WriteLine(sw.ToString());
        }

        private static void CheckArg(string arg, string message) {
            if(string.IsNullOrEmpty(arg)) {
                throw new CliArgException(message);
            }
        }

        private static void PrintErrorAndExit(string message) {
            Console.Error.WriteLine("ERROR: " + message);
            Environment.Exit(-1);
        }

        private static void OutputException(Exception exception) {
            if(exception.InnerException != null) {
                OutputException(exception.InnerException);
            }
            Console.Error.WriteLine("----------------------------------");
            Console.Error.WriteLine(exception);
        }
        
    }
}
