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
using System.Linq;
using System.Text;
using System.ComponentModel;
using CodeXCavator.Engine.Interfaces;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace CodeXCavator.Indexer.ViewModel
{
    /// <summary>
    /// IndexBuilderViewModel class.
    /// 
    /// View model for single index builder instance.
    /// </summary>
    internal class IndexBuilderViewModel : ViewModelBase
    {
        ApplicationViewModel mParent;
        private KeyValuePair<Engine.Interfaces.IIndexBuilder, IEnumerable<string>> mIndexBuilder;

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="parent">Parent application view model.</param>
        /// <param name="indexBuilder">Underlying index builder.</param>
        public IndexBuilderViewModel( ApplicationViewModel parent, KeyValuePair<Engine.Interfaces.IIndexBuilder, IEnumerable<string>> indexBuilder, bool launchImmediately )
        {
            mParent = parent;
            this.mIndexBuilder = indexBuilder;
            Errors = new ObservableCollection<string>();
            if( launchImmediately )
                Launch();
        }

        internal void Launch()
        {
            if( ( (App) Application.Current ).Multithreading )
            {
                // Start a background worker in order to determine the input files, 
                // which should be added to the index.
                // Although this costs some time to iterate over all files,
                // it is done in order to be able to indicate indexing progress later on.
                var numberOfInputFilesEvaluator = new BackgroundWorker();
                numberOfInputFilesEvaluator.WorkerSupportsCancellation = false;
                numberOfInputFilesEvaluator.DoWork += new DoWorkEventHandler( numberOfInputFilesEvaluator_DoWork );
                numberOfInputFilesEvaluator.RunWorkerCompleted += new RunWorkerCompletedEventHandler( numberOfInputFilesEvaluator_RunWorkerCompleted );
                numberOfInputFilesEvaluator.RunWorkerAsync( this );
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke( new Action( () =>
                {
                    var e1 = new DoWorkEventArgs( this );
                    Exception error = null;
                    try
                    {
                        numberOfInputFilesEvaluator_DoWork( this, e1 );
                    }
                    catch( Exception ex )
                    {
                        e1.Result = ex;
                        error = ex;
                    }
                    var e2 = new RunWorkerCompletedEventArgs( e1.Result, error, e1.Cancel );
                    numberOfInputFilesEvaluator_RunWorkerCompleted( this, e2 );
                } ) );
            }
        }

        /// <summary>
        /// Evaluates the number of input files.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        void numberOfInputFilesEvaluator_DoWork( object sender, DoWorkEventArgs e )
        {
            var inputFileEnumerable = mIndexBuilder.Value;
            try
            {
                e.Result = ( inputFileEnumerable != null ) && ( (App) Application.Current ).EstimateProgress ? (object) inputFileEnumerable.Count() : null;
            }
            catch( Exception exception )
            {
                e.Result = exception;
            }
        }

        /// <summary>
        /// Handles completion of input file number evaluation.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void numberOfInputFilesEvaluator_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            if( e.Result is Exception )
            {
                NumberOfInputFiles = 0;
                IndexSize = string.Empty;
                Error = string.Format( "An error occurred while processing index: \"{0}\"!\n{1}", this.mIndexBuilder.Key.IndexPath, e.Result );
                App app = Application.Current as App;
                if( app != null && app.Silent )
                    System.Console.Out.WriteLine( Error );
                IsIndexing = false;
                return;
            }
            // Set the number of input files property.
            NumberOfInputFiles = ( e.Result != null ) ? (Nullable<int>) e.Result : null;

            IsIndexing = true;

            // Start a background worker, which will perform the actual indexing work.
            if( ( (App) Application.Current ).Multithreading )
            {
                var indexBuilder = new BackgroundWorker();
                indexBuilder.WorkerReportsProgress = true;
                indexBuilder.WorkerSupportsCancellation = false;
                indexBuilder.DoWork += new DoWorkEventHandler( indexBuilder_DoWork );
                indexBuilder.ProgressChanged += new ProgressChangedEventHandler( indexBuilder_ProgressChanged );
                indexBuilder.RunWorkerCompleted += new RunWorkerCompletedEventHandler( indexBuilder_RunWorkerCompleted );
                indexBuilder.RunWorkerAsync( this );
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke( new Action( () =>
                {
                    var e1 = new DoWorkEventArgs( this );
                    Exception error = null;
                    try
                    {
                        IsIndexing = true;
                        indexBuilder_DoWork( this, e1 );
                    }
                    catch( Exception ex )
                    {
                        e1.Result = ex;
                        error = ex;
                    }
                    var e2 = new RunWorkerCompletedEventArgs( e1.Result, error, e1.Cancel );
                    indexBuilder_RunWorkerCompleted( this, e2 );
                } ) );                                
            }
        }

        /// <summary>
        /// Builds an index by adding all input files to it.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        void indexBuilder_DoWork( object sender, DoWorkEventArgs e )
        {
            if( sender == this )
            {
                IProgressProvider progressProvider = mIndexBuilder.Key as IProgressProvider;
                if( progressProvider != null )
                {
                    progressProvider.OnProgress += ( object progressSender, string message ) => ReportProgress( 0, message );
                }
            }
            else
            {
                BackgroundWorker worker = sender as BackgroundWorker;
                if( worker != null )
                {
                    IProgressProvider progressProvider = mIndexBuilder.Key as IProgressProvider;
                    if( progressProvider != null )
                    {
                        progressProvider.OnProgress += ( object progressSender, string message ) => worker.ReportProgress( 0, message );
                    }
                }
            }
            // Add all input files to the index. As the index builder implements the IProgressPRovider interface,
            // we will be notified of each file being added to the index.
            mIndexBuilder.Key.AddFiles( mIndexBuilder.Value );
            mIndexBuilder.Key.Dispose();
        }

        /// <summary>
        /// Report progress using current application dispatcher.
        /// </summary>
        /// <param name="progress">Progress in percent.</param>
        /// <param name="message">Progress message.</param>
        private void ReportProgress( int progress, string message )
        {
            Application.Current.Dispatcher.BeginInvoke( new Action( () => indexBuilder_ProgressChanged( this, new ProgressChangedEventArgs( 0, message ) ) ) );
            Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
        }
        
        /// <summary>
        /// Handles index builder progress change.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        void indexBuilder_ProgressChanged( object sender, ProgressChangedEventArgs e )
        {
            Progress = Progress + 1;
            if( !( (App) Application.Current ).EstimateProgress )
                NumberOfInputFiles = Progress;
            CurrentFile = (string) e.UserState;
            App app = Application.Current as App;
            string progressMessage = e.UserState.ToString();
            if( app != null && app.Silent )
                System.Console.Out.WriteLine( progressMessage );
            if( e.UserState.ToString().IndexOf( "error", StringComparison.OrdinalIgnoreCase ) == 0 )
            {
                Errors.Add( progressMessage );
                if( Error == null )
                    Error = string.Format( "One or more errors occurred while processing index: \"{0}\"!\nClick to view error log...", mIndexBuilder.Key.IndexPath );
            }
        }

        /// <summary>
        /// Handles completion of index build process.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        void indexBuilder_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            IsIndexing = false;

            if( ( (App) Application.Current ).Multithreading )
            {
                // Start background worker in order to determine the index size.
                var indexSizeEvaluator = new BackgroundWorker();
                indexSizeEvaluator.WorkerSupportsCancellation = false;
                indexSizeEvaluator.DoWork += new DoWorkEventHandler( indexSizeEvaluator_DoWork );
                indexSizeEvaluator.RunWorkerCompleted += new RunWorkerCompletedEventHandler( indexSizeEvaluator_RunWorkerCompleted );
                indexSizeEvaluator.RunWorkerAsync( this );
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke( new Action( () =>
                {
                    var e1 = new DoWorkEventArgs( this );
                    Exception error = null;
                    try
                    {
                        indexSizeEvaluator_DoWork( this, e1 );
                    }
                    catch( Exception ex )
                    {
                        e1.Result = ex;
                        error = ex;
                    }
                    var e2 = new RunWorkerCompletedEventArgs( e1.Result, error, e1.Cancel );
                    indexSizeEvaluator_RunWorkerCompleted( this, e2 );
                } ) );
            }
        }

        /// <summary>
        /// Evaluates the current index size.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        void indexSizeEvaluator_DoWork( object sender, DoWorkEventArgs e )
        {
            try
            {
                // Wait a little bit, as Lucene may not yet have deleted to previous index, which 
                // would result in an invalid index size.
                System.Threading.Thread.Sleep( 250 );

                long size = 0;

                // Sum up the size of all files contained in the index directory.
                foreach( var file in System.IO.Directory.EnumerateFiles( mIndexBuilder.Key.IndexPath, "*.*", System.IO.SearchOption.AllDirectories ) )
                {
                    var fileInfo = new System.IO.FileInfo( file );
                    size += fileInfo.Length;
                }

                e.Result = size;
            }
            catch
            {
                e.Result = -1;
            }            
        }

        /// <summary>
        /// Handles completion of index size evaluation.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        void indexSizeEvaluator_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            long sizeInBytes = (long) e.Result;
            if( sizeInBytes >= 0 )
            {
                // Set index size to evaluated value.
                IndexSize = ( ( sizeInBytes ) / ( 1024.0 * 1024.0 ) ).ToString( "F3" ) + "MB";
            }
            else
            {
                // Could not properly determine index size.
                IndexSize = "unknown";
            }
            // Signalize that the index builder is finished.
            IsFinished = true;
        }

        /// <summary>
        /// Returns the full path of the index.
        /// </summary>
        public string Path
        {
            get
            {
                return mIndexBuilder.Key != null ? mIndexBuilder.Key.IndexPath : string.Empty;
            }
        }

        /// <summary>
        /// Returns the name of the index.
        /// </summary>
        public string Name
        {
            get
            {
                return mIndexBuilder.Key != null ? System.IO.Path.GetFileName( mIndexBuilder.Key.IndexPath ) : string.Empty;
            }
        }

        private int? mNumberOfInputFiles;

        /// <summary>
        /// Returns the number of input files for the index.
        /// 
        /// This is intentionally Nullable. If the value is null, the system
        /// displays an in-progress indicator instead of the number of input files,
        /// signalizing that the value is being evaluated.
        /// </summary>
        public int? NumberOfInputFiles
        {
            get
            {
                return mNumberOfInputFiles;
            }
            private set
            {
                if( mNumberOfInputFiles != value )
                {
                    mNumberOfInputFiles = value;
                    OnPropertyChanged( "NumberOfInputFiles" );
                }                
            }
        }

        private int mProgress;

        /// <summary>
        /// Current indexing progress.
        /// 
        /// This returns the number of files which have already been processed.
        /// </summary>
        public int Progress
        {
            get
            {
                return mProgress;
            }
            private set
            {
                if( mProgress != value )
                {
                    mProgress = value;
                    OnPropertyChanged( "Progress" );
                }
            }
        }

        private bool mIsIndexing;

        /// <summary>
        /// Returns, whether the underlying index builder is
        /// currently indexing.
        /// </summary>
        public bool IsIndexing
        {
            get
            {
                return mIsIndexing;
            }
            private set
            {
                if( mIsIndexing != value )
                {
                    mIsIndexing = value;
                    if( mParent != null )
                    {
                        if( mIsIndexing )
                            mParent.IndexBuildersActive++;
                        else
                            mParent.IndexBuildersActive--;
                    }
                    OnPropertyChanged( "IsIndexing" );
                }
            }
        }

        private bool mIsFinished;

        public bool IsFinished
        {
            get
            {
                return mIsFinished;
            }
            set
            {
                if( value != mIsFinished )
                {
                    mIsFinished = value;
                    if( mParent != null )
                    {
                        if( mIsFinished )
                            mParent.IndexBuildersFinished++;
                        else
                            mParent.IndexBuildersFinished--;
                    }
                    OnPropertyChanged( "IsFinished" );
                }
            }
        }

        private string mCurrentFile;

        /// <summary>
        /// Current file being indexed.
        /// </summary>
        public string CurrentFile
        {
            get
            {
                return mCurrentFile;
            }
            private set
            {
                if( mCurrentFile != value )
                {
                    mCurrentFile = value;
                    OnPropertyChanged( "CurrentFile" );
                }
            }
        }

        private string mIndexSize;

        /// <summary>
        /// Current index size.
        /// 
        /// This is intentionally a string, as it also contains the unit ( MB ). Further more, if this
        /// value is null, the system displays an in-progress indicator, signalizing that the value
        /// is being evaluated.
        /// </summary>
        public string IndexSize
        {
            get
            {
                return mIndexSize;
            }
            set
            {
                if( mIndexSize != value )
                {
                    mIndexSize = value;
                    OnPropertyChanged( "IndexSize" );
                }
            }
        }

        private string mError;

        /// <summary>
        /// Error occurred during indexing.
        /// </summary>
        public string Error
        {
            get
            {
                return mError;
            }
            set
            {
                if( mError != value )
                {
                    mError = value;
                    OnPropertyChanged( "Error" );
                }
            }
        }

				/// <summary>
				/// List of additional errors occurred during indexing.
				/// </summary>
        public ObservableCollection<string> Errors
        {
            get;
            private set;
        }
    }
}
