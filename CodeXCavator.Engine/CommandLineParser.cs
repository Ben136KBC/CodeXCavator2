// Copyright 2015 Christoph Brzozowski
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// Command line parser class
    /// 
    /// This class parses the command line arguments for the indexer and the search tool.
    /// </summary>
    public class CommandLineParser
    {
        private HashSet<string> mSwitches = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
        private Dictionary<string,string> mSwitchValues = new Dictionary<string,string>( StringComparer.OrdinalIgnoreCase );
        private HashSet<string> mIndexConfigurationFiles = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
        private HashSet<string> mIndexConfigurationListFiles = new HashSet<string>( StringComparer.OrdinalIgnoreCase );
        private HashSet<string> mIndexConfigurationDirectories = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// Help command line switches
        /// </summary>
        public static readonly string[] HELP_SWITCHES = new string[] { "?", "help" };
        /// <summary>
        /// Auto close command line switches
        /// </summary>
        public static readonly string[] AUTO_CLOSE_SWITCHES = new string[] { "autoclose" };
        /// <summary>
        /// Silent mode command line switches
        /// </summary>
        public static readonly string[] SILENT_SWITCHES = new string[] { "nogui", "silent" };
        /// <summary>
        /// Multithrading switches
        /// </summary>
        public static readonly string[] NO_MULTI_THREADING_SWITCHES = new string[] { "nomultithreading", "nothreading", "nothread" };
        /// <summary>
        /// Progress switches
        /// </summary>
        public static readonly string[] NO_PROGRESS_SWITCHES = new string[] { "noprogress" };
        /// <summary>
        /// Maximum number of workers switches
        /// </summary>
        public static readonly string[] MAX_WORKER_SWITCHES = new string[] { "maxworkers" };

        /// <summary>
        /// Parses command line arguments.
        /// </summary>
        /// <param name="commandLineArguments">List of command line arguments.</param>
        public void ParseCommandLineArguments( IEnumerable<string> commandLineArguments )
        {
            mSwitches.Clear();
            mIndexConfigurationFiles.Clear();
            mIndexConfigurationListFiles.Clear();
            mIndexConfigurationDirectories.Clear();

            // Gather switches and configuration files
            foreach( var arg in commandLineArguments )
            {
                if( IsSwitch( arg ) )
                {
                    var switchNameAndValue = GetSwitchNameAndValue( arg );
                    mSwitches.Add( switchNameAndValue.Key );
                    if( switchNameAndValue.Value != null )
                        mSwitchValues[switchNameAndValue.Key] = switchNameAndValue.Value;
                }
                else
                if( IsConfigurationFile( arg ) )
                    mIndexConfigurationFiles.Add( GetAbsolutePath( arg ) );
                else
                if( IsIndexListFile( arg ) )
                    mIndexConfigurationListFiles.Add( GetAbsolutePath( arg ) );
                else
                    mIndexConfigurationDirectories.Add( GetAbsolutePath( arg ) );
            }

            AddIndexConfigurationFilesFromLists();
            AddIndexConfigurationFilesFromDirectories();
        }

        /// <summary>
        /// List of all switches.
        /// </summary>
        public IEnumerable<string> Switches { get { return mSwitches; } }
        /// <summary>
        /// List of index configuration files.
        /// </summary>
        public IEnumerable<string> IndexConfigurationFiles { get { return mIndexConfigurationFiles; } }
        /// <summary>
        /// List of directories containing index configuration files.
        /// </summary>
        public IEnumerable<string> IndexConfigurationDirectories { get { return mIndexConfigurationDirectories; } }
        /// <summary>
        /// List of files containing lists of index configuration files.
        /// </summary>
        public IEnumerable<string> IndexConfigurationListFiles { get { return mIndexConfigurationListFiles; } }

        /// <summary>
        /// Checks, whether the specified switch has been set.
        /// </summary>
        /// <param name="switch">Command line switch, which should be checked.</param>
        /// <returns>True, if the command line switch has been specified, false otherwise.</returns>
        public bool HasSwitch( params string[] switches ) 
        {
            return switches.Any( @switch => mSwitches.Contains( @switch ) );
        }

        /// <summary>
        /// Returns the value of the given switch.
        /// </summary>
        /// <param name="switches">Switch names to probe.</param>
        /// <returns>Value of the switch, or null, if not set.</returns>
        public string GetSwitchValue( params string[] switches )
        {
            foreach( var switchName in switches )
            {
                string value;
                if( mSwitchValues.TryGetValue( switchName, out value ) )
                    return value;
            }
            return null;
        }

        /// <summary>
        /// Checks, whether the specified argument is a switch.
        /// </summary>
        /// <param name="arg">Argument, which should be tested for being a switch.</param>
        /// <returns>True, if the command line argument is a switch, false otherwise.</returns>
        private static bool IsSwitch( string arg )
        {
            return arg.StartsWith( "/" ) || arg.StartsWith( "-" );
        }

        /// <summary>
        /// Returns the name of the switch.
        /// </summary>
        /// <param name="arg">Switch argument, whose name should be returned.</param>
        /// <returns>Name of the switch.</returns>
        private static KeyValuePair<string, string> GetSwitchNameAndValue( string arg )
        {
            int valuePos = arg.IndexOf( '=' );
            if( valuePos < 0 )
                valuePos = arg.Length;
            var name = arg.Substring( 1, valuePos - 1 );
            var value = valuePos < arg.Length - 1 ? arg.Substring( valuePos + 1 ) : null;
            return new KeyValuePair<string, string>( name, value );
        }

        /// <summary>
        /// Checks, whether the specified arg is a configuration file.
        /// </summary>
        /// <param name="arg">Argument, which should be checked.</param>
        /// <returns>True, if the argument references an index configuration file.</returns>
        private static bool IsConfigurationFile( string arg )
        {
            return System.IO.Path.GetExtension( arg ).Equals( ".xml", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Checks, whether the specified arg is an index list file.
        /// </summary>
        /// <param name="arg">Argument, which should be checked.</param>
        /// <returns>True, if the argument references an index list file.</returns>
        private static bool IsIndexListFile( string arg )
        {
            return System.IO.Path.GetExtension( arg ).Equals( ".lst", StringComparison.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Checks, whether a line is a comment line.
        /// </summary>
        /// <param name="line">Line, which should be checked.</param>
        /// <returns>True, if the line starts with # or ', false otherwise</returns>
        private static bool IsCommentLine( string line ) 
        {
            foreach( var c in line )
            {
                if( !char.IsWhiteSpace( c ) )
                    return ( c == '#' || c == '\'' );
            }
            return false;
        }

        /// <summary>
        /// Adds all configuration files from all specified lists.
        /// </summary>
        void AddIndexConfigurationFilesFromLists()
        {
            foreach( var indexConfigurationListFile in mIndexConfigurationListFiles )
            {
                if( !File.Exists( indexConfigurationListFile ) )
                    throw new ArgumentException( string.Format( "Index configuration list file '{0}' does not exist!", indexConfigurationListFile ) );
                foreach( var line in File.ReadLines( indexConfigurationListFile ) )
                {
                    AddIndexConfigurationFileFromListEntry( indexConfigurationListFile, line );
                }
            }
        }

        private void AddIndexConfigurationFileFromListEntry( string indexConfigurationListFile, string listEntry )
        {
            if( !string.IsNullOrWhiteSpace( listEntry ) && !IsCommentLine( listEntry ) )
            {
                var indexConfigurationFile = listEntry.Trim();
                if( !Path.IsPathRooted( indexConfigurationFile ) )
                {
                    var indexConfigurationListDirectory = Path.GetDirectoryName( indexConfigurationListFile );
                    indexConfigurationFile = Path.Combine( indexConfigurationListDirectory, indexConfigurationFile );
                }
                mIndexConfigurationFiles.Add( GetAbsolutePath( indexConfigurationFile ) );
            }
        }

        /// <summary>
        /// Adds all configuration files from all directories.
        /// </summary>
        void AddIndexConfigurationFilesFromDirectories()
        {
            foreach( var indexConfigurationDirectory in mIndexConfigurationDirectories )
            {
                if( !Directory.Exists( indexConfigurationDirectory ) )
                    throw new ArgumentException( string.Format( "Index configuration file directory '{0}' does not exist!", indexConfigurationDirectory ) );
                foreach( var indexConfigurationFile in Directory.EnumerateFiles( indexConfigurationDirectory, "*.xml" ) )
                    mIndexConfigurationFiles.Add( GetAbsolutePath( indexConfigurationFile ) );
            }
        }

        /// <summary>
        /// Returns the absolute path of a file path.
        /// </summary>
        /// <param name="path">Path to a file or directory.</param>
        /// <returns>Absolute path of the file or directory.</returns>
        private static string GetAbsolutePath( string path )
        {
            try
            {
                return Path.GetFullPath( path );
            }
            catch
            {
                return path;
            }
        }
   }
}