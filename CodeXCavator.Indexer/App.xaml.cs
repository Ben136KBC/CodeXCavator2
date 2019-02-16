// Copyright 2014 Christoph Brzozowski
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
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using System.IO;
using CodeXCavator.Engine.Interfaces;
using CodeXCavator.Engine;

namespace CodeXCavator.Indexer
{
    /// <summary>
    /// Application class.
    /// </summary>
    public partial class App : Application
    {
        internal IEnumerable< KeyValuePair< IIndexBuilder, IEnumerable< string > > > IndexBuilders { get; private set; }
        private CommandLineParser mCommandLineParser;
        private const string MESSAGE_BOX_CAPTION = "CodeXCavator - Indexer...";

        /// <summary>
        /// Handles the OnStartup event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );

            mCommandLineParser = new CommandLineParser();
         
            try
            {
                mCommandLineParser.ParseCommandLineArguments( e.Args );
            }
            catch( Exception ex )
            {
                Message( ex.Message, MESSAGE_BOX_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error );
                this.Shutdown();
                return;
            }
            
            // Check, whether display usage box
            if( mCommandLineParser.HasSwitch( CommandLineParser.HELP_SWITCHES ) )
            {
                Message( "Usage:\n\tCodeXCavator [switches] <index_configuration_files>\n" +
                         "Switches:\n\t-help\t\tDisplays a help message box.\n" +
                         "\t-autoclose\tClose when finished.\n" +
                         "\t-silent\t\tSilent mode without user interface.\n" + 
                         "\t-noprogress\tDon't estimate progress.\n" +
                         "\t-nomultithreading\tDon't use multithreading for indexing.\n" +
                         "\t-maxworkers=<number|CPU>\tUser <number> of workers for indexing.\n" +
                         "\t                        \tIf set to 'CPU' the workers are limited by number of CPU cores.",
                         MESSAGE_BOX_CAPTION, MessageBoxButton.OK, MessageBoxImage.Information );
                this.Shutdown();
                return;
            }

            // Check, whether at least one index configuration file has been specified as command line argument.
            if( !mCommandLineParser.IndexConfigurationFiles.Any() )
            {
                Message( "No index source configuration file specified!\nYou have to specify at least a single XML configuration file, which defines the input files to be indexed.", 
                         MESSAGE_BOX_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error );
                Application.Current.Shutdown();
                return;
            }

            // Create an index builder for each specified index configuration.
            var indexBuilders = new List< KeyValuePair< IIndexBuilder, IEnumerable< string > > >();
            foreach( var configurationFile in mCommandLineParser.IndexConfigurationFiles )
            {
                if( !System.IO.File.Exists( configurationFile ) )
                {
                    Message( string.Format( "Source configuration file \"{0}\" does not exist!", configurationFile ), "CodeXCavator - Indexer...", MessageBoxButton.OK, MessageBoxImage.Error );
                    continue;
                }
                try
                {
                    IEnumerable<string> indexBuilderSourceFiles;
                    // Create index builder from the XML configuration file.
                    IIndexBuilder indexBuilder = IIndexBuilderExtensions.CreateFromXmlFile( configurationFile, out indexBuilderSourceFiles );
                    if( indexBuilder != null )
                    {
                        // Add index builder and enumeration of input files to the list of builders.
                        indexBuilders.Add( new KeyValuePair<IIndexBuilder,IEnumerable<string>>( indexBuilder, indexBuilderSourceFiles ) );
                    }
                    else
                    {
                        Message( string.Format( "Could not initialize index builder from configuration file \"{0}\"!", configurationFile ), "CodeXCavator - Indexer...", MessageBoxButton.OK, MessageBoxImage.Error );
                    }
                }
                catch( Exception exception )
                {
                    Message( string.Format( "Unexpected error occurred while reading configuration file \"{0}\"!\n\n{1}", configurationFile, exception.ToString() ), "CodeXCavator - Indexer...", MessageBoxButton.OK, MessageBoxImage.Error );
                    Application.Current.Shutdown();
                    return;
                }
            }

            IndexBuilders = indexBuilders;
        }

        /// <summary>
        /// Handles OnExit event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnExit( ExitEventArgs e )
        {
            // Dispose all builders.
            if( IndexBuilders != null )
            {
                foreach( var indexBuilder in IndexBuilders )
                    indexBuilder.Key.Dispose();
            }
            base.OnExit( e );
        }

        /// <summary>
        /// Determines, whether application should close, when finished.
        /// </summary>
        public bool ShouldCloseWhenIndexingFinished 
        { 
            get
            {
                return mCommandLineParser.HasSwitch( CommandLineParser.AUTO_CLOSE_SWITCHES ) || Silent;
            }
        }

        /// <summary>
        /// Determines, whether the application should run in silent mode.
        /// </summary>
        public bool Silent
        {
            get
            {
                return mCommandLineParser.HasSwitch( CommandLineParser.SILENT_SWITCHES );
            }
        }

        /// <summary>
        /// Determines, whether progress should be estimated.
        /// </summary>
        public bool EstimateProgress
        {
            get
            {
                return !mCommandLineParser.HasSwitch( CommandLineParser.NO_PROGRESS_SWITCHES );
            }            
        }

        /// <summary>
        /// Determines, whether multithreading should be used for indexing.
        /// </summary>
        public bool Multithreading
        {
            get
            {
                return !mCommandLineParser.HasSwitch( CommandLineParser.NO_MULTI_THREADING_SWITCHES );
            }
        }

        /// <summary>
        /// Returns the maximum number of indexing workers.
        /// </summary>
        public int MaxIndexingWorkers
        {
            get
            {
                var maxWorkersValue = mCommandLineParser.GetSwitchValue( CommandLineParser.MAX_WORKER_SWITCHES );
                if( maxWorkersValue == null )
                    return int.MaxValue;
                maxWorkersValue = maxWorkersValue.Trim();
                if( string.Equals( maxWorkersValue, "CPU", StringComparison.OrdinalIgnoreCase ) )
                    return Environment.ProcessorCount;
                int maxWorkers;
                if( int.TryParse( maxWorkersValue, out maxWorkers ) )
                    return maxWorkers;
                return int.MaxValue;
            }
        }
        
        /// <summary>
        /// Displays a message box, or outputs a message on the standard output.
        /// </summary>
        /// <param name="message">Message.</param>
        /// <param name="caption">Caption.</param>
        /// <param name="messageBoxButton">Message box buttons.</param>
        /// <param name="messageBoxImage">Message box image.</param>
        /// <returns>Message box result.</returns>
        internal MessageBoxResult Message( string message, string caption, MessageBoxButton messageBoxButton, MessageBoxImage messageBoxImage )
        {
            if( Silent )
            {
                if( messageBoxImage == MessageBoxImage.Error )
                    Console.Error.WriteLine( message );
                else
                    Console.Out.WriteLine( message );
                return MessageBoxResult.OK;
            }
            else
            {
                return MessageBox.Show( message, caption, messageBoxButton, messageBoxImage );
            }
        }
    }
}
