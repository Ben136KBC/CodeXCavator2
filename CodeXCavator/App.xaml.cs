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
using CodeXCavator.Engine.Interfaces;
using CodeXCavator.Engine;

namespace CodeXCavator
{
    /// <summary>
    /// Application class.
    /// </summary>
    public partial class App : Application
    {
        private const string MESSAGE_BOX_CAPTION = "CodeXCavator - Searcher...";

        internal IEnumerable<IIndex> Indexes { get; private set; }

        /// <summary>
        /// Handles the OnStartup event.
        /// </summary>
        /// <param name="e">Event args</param>
        protected override void OnStartup( StartupEventArgs e )
        {
            base.OnStartup( e );            
         
            var commandLineParser = new CommandLineParser();
            try
            {
                commandLineParser.ParseCommandLineArguments( e.Args );
            }
            catch( Exception ex )
            {
                MessageBox.Show( ex.Message, MESSAGE_BOX_CAPTION, MessageBoxButton.OK, MessageBoxImage.Error );
                Application.Current.Shutdown();
                return;
            }

            var indexes = new List<IIndex>();

            // Create an index for each specified index configuration.
            foreach( var indexConfigurationFile in commandLineParser.IndexConfigurationFiles )
            {
                if( !System.IO.File.Exists( indexConfigurationFile ) )
                {
                    MessageBox.Show( string.Format( "Index configuration file \"{0}\" does not exist!", indexConfigurationFile ), "CodeXCavator - Searcher...", MessageBoxButton.OK, MessageBoxImage.Information );
                    continue;
                }
                try
                {
                    // Create index from the XML configuration file.
                    IIndex index = IIndexExtensions.CreateFromXmlFile( indexConfigurationFile );
                    if( index != null )
                    {
                        CodeXCavator.UI.MRUHandler.PushMRUFile(indexConfigurationFile);
                        // Add index to list of indexes.
                        indexes.Add( index );
                    }
                    else
                    {
                        MessageBox.Show( string.Format( "Could not initialize index from configuration file \"{0}\"!", indexConfigurationFile ), "CodeXCavator - Searcher...", MessageBoxButton.OK, MessageBoxImage.Information );
                    }
                }
                catch( Exception exception )
                {
                    MessageBox.Show( string.Format( "Unexpected error occurred while reading index configuration file \"{0}\"!\n\n{1}", indexConfigurationFile, exception.ToString() ), "CodeXCavator - Searcher...", MessageBoxButton.OK, MessageBoxImage.Error );
                    if( Application.Current != null )
                        Application.Current.Shutdown();
                }
            }

            Indexes = indexes;
        }

        /// <summary>
        /// Handles OnExit event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnExit( ExitEventArgs e )
        {
            // Dispose all indexes.
            if( Indexes != null )
                foreach( var index in Indexes )
                    index.Dispose();
            base.OnExit( e );
        }

    }
}
