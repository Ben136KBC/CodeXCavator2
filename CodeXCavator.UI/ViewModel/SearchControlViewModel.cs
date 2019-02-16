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
using System.Collections.ObjectModel;
using System.Windows.Threading;
using CodeXCavator.Engine.Interfaces;
using System.Windows;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// SearchControlViewModel class.
    /// 
    /// View model for the actual search control.
    /// </summary>
    internal class SearchControlViewModel : ViewModelBase
    {
        private UserSettings mUserSettings;
        IEnumerable<IIndex> mIndexes;

        private SearchViewModel mCurrentSearch;
        private ObservableCollection<SearchViewModel> mSearches;
        private Command mAddSearchCommand;
        private Command mRemoveSearchCommand;

        private ObservableCollection<DocumentViewModel> mOpenDocuments;
        private IFileAction mCloseAllButThisFileAction;
        private IFileAction mCloseAllFilesAction;
        private DataTemplate mFileViewerTemplate;

        /// <summary>
        /// Returns the current user settings.
        /// </summary>
        internal UserSettings UserSettings { get { return mUserSettings; } }
        
        /// <summary>
        /// File viewer data template.
        /// 
        /// This template is used by the document view model, in order to display the document.
        /// This is needed to prevent tab virtualization on the file viewer tab control.
        /// </summary>
        public DataTemplate FileViewerTemplate
        {
            get { return mFileViewerTemplate; }
            set 
            {
                if( mFileViewerTemplate != value )
                {
                    mFileViewerTemplate = value;
                    foreach( var openDocument in OpenDocuments )
                        openDocument.FileViewerTemplate = mFileViewerTemplate;
                }
            }
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="indexes">List of underlying indexes, on which search can be performed.</param>
        public SearchControlViewModel( IEnumerable< IIndex > indexes, UserSettings userSettings )        
        {
            mIndexes = indexes;
            mUserSettings = userSettings;

            InitializeSearches();
            InitializeOpenDocuments();
        }

        private void InitializeOpenDocuments()
        {
            mOpenDocuments = new ObservableCollection<DocumentViewModel>();
            mCloseAllButThisFileAction = new CodeXCavator.Engine.FileAction( "FileActionCloseAllFilesButThis", "Close all files but this", "Closes all opened files but this file.", CanCloseAllFilesButThis, CloseAllFilesButThis );
            mCloseAllFilesAction = new CodeXCavator.Engine.FileAction( "FileActionCloseAll", "Close all files", "Closes all opened files.", CanCloseAllFiles, CloseAllFiles );
        }

        private void InitializeSearches()
        {
            mSearches = new ObservableCollection<SearchViewModel>();
            mSearches.Add( new SearchViewModel( mIndexes, mUserSettings ) );
            mSearches.Add( null );
            mCurrentSearch = mSearches[0];

            mAddSearchCommand = new Command( CanAddSearch, AddSearch );
            mRemoveSearchCommand = new Command( CanRemoveSearch, RemoveSearch );
        }

        /// <summary>
        /// Returns an enumeration of index view models for the underlying indexes.
        /// </summary>
        public IEnumerable<IndexViewModel> Indexes
        {
            get
            {
                foreach( var index in mIndexes )
                    yield return new IndexViewModel( index );
            }
        }               

        /// <summary>
        /// Enumeration of documents being opened in the document viewer.
        /// </summary>
        public ObservableCollection<DocumentViewModel> OpenDocuments
        {
            get { return mOpenDocuments; }
        }

        protected DocumentViewModel mCurrentDocument;
        
        /// <summary>
        /// Document, which is currently active in the document viewer.
        /// </summary>
        public DocumentViewModel CurrentDocument
        {
            get { return mCurrentDocument; }
            set
            {
                if( mCurrentDocument != value )
                {
                    mCurrentDocument = value;
                    OnPropertyChanged( "CurrentDocument" );
                }
            }
        }

        /// <summary>
        /// Checks, whether all files can be closed.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool CanCloseAllFiles( string filePath )
        {
            return OpenDocuments.Count > 0;
        }

        /// <summary>
        /// Closes all opened files.
        /// </summary>
        /// <param name="filePath"></param>
        private void CloseAllFiles( string filePath )
        {
            if( CanCloseAllFiles( filePath ) )
            {
                foreach( var filePathOfDocumentToClose in OpenDocuments.Select( openDocument => openDocument.FilePath ).ToArray() )
                    CloseDocument( filePathOfDocumentToClose );
            }
        }

        
        /// <summary>
        /// Checks, whether all files but the specified one can be closed.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool CanCloseAllFilesButThis( string filePath )
        {            
            return OpenDocuments.Count > 1 && !string.IsNullOrEmpty( filePath );
        }

        /// <summary>
        /// Closes all opened files but the specified one.
        /// </summary>
        /// <param name="filePath">File, which should remain opened.</param>
        private void CloseAllFilesButThis( string filePath )
        {
            if( CanCloseAllFilesButThis( filePath ) )
            {
                foreach( var filePathOfDocumentToClose in OpenDocuments.Select( openDocument => openDocument.FilePath ).Where( openDocumentFilePath => !openDocumentFilePath.Equals( filePath, StringComparison.OrdinalIgnoreCase ) ).ToArray() )
                    CloseDocument( filePathOfDocumentToClose );
            }
        }

        /// <summary>
        /// Opens a document in the viewer.
        /// 
        /// If the document is already opened it is just activated.
        /// </summary>
        /// <param name="filePath">Path of the document, which should be opened in the viewer.</param>
        /// <returns>Opened document view model.</returns>
        public DocumentViewModel OpenDocument( string filePath )
        {
            if( Engine.FileStorageProviders.Exists( filePath ) )
            {
                try
                {
                    System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                    foreach( var openDocument in OpenDocuments )
                    {
                        if( openDocument.FilePath.Equals( filePath, StringComparison.OrdinalIgnoreCase ) )
                        {
                            return openDocument;
                        }
                    }
                    var newDocument = new DocumentViewModel( filePath, fileActionCloseAllButThis: mCloseAllButThisFileAction, fileActionCloseAll: mCloseAllFilesAction );
                    newDocument.FileViewerTemplate = mFileViewerTemplate;
                    OpenDocuments.Add( newDocument );
                    CurrentDocument = newDocument;
                    return newDocument;
                }
                finally
                {
                    System.Windows.Input.Mouse.OverrideCursor = null;
                }
            }
            else
            {
                System.Windows.MessageBox.Show( string.Format( "Cannot open file \"{0}\" because it does not exist!", filePath ), "Open file...", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
            }
            return null;
        }

        /// <summary>
        /// Opens a document in the viewer.
        /// 
        /// If the document is already opened it is just activated.
        /// </summary>
        /// <param name="filePath">Path of the document, which should be opened in the viewer.</param>
        /// <param name="searchQuery">Search query for which the match occurrences have been returned.</param>
        /// <param name="occurrences">Match occurrences, which should be highlighted in the document viewer.</param>
        /// <returns>Opened document view model.</returns>
        internal DocumentViewModel OpenDocument( string filePath, string searchQuery, ReadOnlyCollection<IOccurrence> occurrences )
        {
            int docIndex = 0;
            if( Engine.FileStorageProviders.Exists( filePath ) )
            {
                try
                {
                    System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;
                    foreach( var openDocument in OpenDocuments )
                    {
                        if( openDocument.FilePath.Equals( filePath, StringComparison.OrdinalIgnoreCase ) )
                            break;
                        ++docIndex;
                    }
                    
                    if( docIndex >= OpenDocuments.Count || OpenDocuments[ docIndex ].OccurrenceModels != occurrences )
                    {
                        var newDocument = new DocumentViewModel( filePath, occurrences, searchQuery, fileActionCloseAllButThis: mCloseAllButThisFileAction, fileActionCloseAll: mCloseAllFilesAction );
                        newDocument.FileViewerTemplate = mFileViewerTemplate;
                        if( docIndex < OpenDocuments.Count )
                            OpenDocuments.RemoveAt( docIndex );
                        OpenDocuments.Insert( docIndex, newDocument );
                        CurrentDocument = newDocument;
                    }
                    else
                    {
                        CurrentDocument = OpenDocuments[ docIndex ]; 
                    }
                    return CurrentDocument;
                }
                finally
                {
                    System.Windows.Input.Mouse.OverrideCursor = null;
                }
            }
            else
            {
                System.Windows.MessageBox.Show( string.Format( "Cannot open file \"{0}\" because it does not exist!", filePath ), "Open file...", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
            }

            return null;
        }

        /// <summary>
        /// Opens a document in the viewer.
        /// 
        /// If the document is already opened it is just activated.
        /// </summary>
        /// <param name="filePath">Path of the document, which should be opened in the viewer.</param>
        /// <param name="searchQuery">Search query for which the match occurrences have been returned.</param>
        /// <param name="occurrences">Match occurrences, which should be highlighted in the document viewer.</param>
        /// <param name="occurrenceToNavigateTo">Occurrence to which the document view should navigate.</param>
        /// <returns>Opened document view model.</returns>
        internal DocumentViewModel OpenDocument( string filePath, string searchQuery, ReadOnlyCollection<IOccurrence> occurrences, IOccurrence occurrenceToNavigateTo )
        {
            var document = OpenDocument( filePath, searchQuery, occurrences );
            if( document != null )
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    new Action( () =>
                        {
                            document.CurrentOccurrence = new OccurrenceViewModel( occurrenceToNavigateTo, null );
                        }
                    ), DispatcherPriority.Background
                );
            }
            return document;
        }

        /// <summary>
        /// Closes a document in the document viewer.
        /// </summary>
        /// <param name="filePath">Path of the document, which should be closed.</param>
        public void CloseDocument( string filePath )
        {
            try
            {
                System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Wait;

                DocumentViewModel documentToRemove = null;
                foreach( var openDocument in OpenDocuments )
                {
                    if( openDocument.FilePath.Equals( filePath, StringComparison.OrdinalIgnoreCase ) )
                    {
                        documentToRemove = openDocument;
                        break;
                    }
                }
                if( documentToRemove != null )
                    OpenDocuments.Remove( documentToRemove );
            }
            finally
            {
                System.Windows.Input.Mouse.OverrideCursor = null;
            }
        }

        /// <summary>
        /// Current active search.
        /// </summary>
        public SearchViewModel CurrentSearch
        {
            get { return mCurrentSearch; }
            set 
            { 
                if( mCurrentSearch != value ) 
                {
                    mCurrentSearch = value;
                    OnPropertyChanged( "CurrentSearch" );
                }
            }
        }
        
        /// <summary>
        /// List of active searches.
        /// </summary>
        public ObservableCollection<SearchViewModel> Searches
        {
            get { return mSearches; }
        }

        /// <summary>
        /// Command for adding new searches.
        /// </summary>
        public Command AddSearchCommand
        {
            get { return mAddSearchCommand; }
        }

        /// <summary>
        /// Determines whether a search can be added.
        /// </summary>
        /// <param name="search">Search to be added.</param>
        /// <returns>True, if search can be added.</returns>
        private bool CanAddSearch( object search )
        {
            return true;
        }

        /// <summary>
        /// Addes a new search.
        /// </summary>
        /// <param name="search">Search to be added.</param>
        private void AddSearch( object search )
        {
            SearchViewModel searchToBeAdded = search == null ? new SearchViewModel( mIndexes, mUserSettings ) : search as SearchViewModel;
            if( searchToBeAdded != null )
            {
                mSearches.Insert( mSearches.Count - 1, searchToBeAdded );
                CurrentSearch = searchToBeAdded;
            }
        }

        /// <summary>
        /// Command for removing existing searches.
        /// </summary>
        public Command RemoveSearchCommand
        {
            get { return mRemoveSearchCommand; }
        }

        /// <summary>
        /// Determines, whether a search can be removed.
        /// </summary>
        /// <param name="search">Search to be removed.</param>
        /// <returns>True, if search can be remove.</returns>
        private bool CanRemoveSearch( object search )
        {
            return search != null && mSearches.Count > 2;
        }

        /// <summary>
        /// Removes a search.
        /// </summary>
        /// <param name="search">Search to be removed.</param>
        private void RemoveSearch( object search )
        {
            var searchToBeRemoved = search as SearchViewModel;
            try
            {
                if( searchToBeRemoved != null )
                    mSearches.Remove( searchToBeRemoved );
            }
            catch
            {
            }
        }

        /// <summary>
        /// Searches for the given content.
        /// </summary>
        /// <param name="content">Content to search.</param>
        internal void SearchContent( string content )
        {
            var currentSearchViewModel = CurrentSearch;
            if( currentSearchViewModel == null )
                return;

            if( !string.IsNullOrEmpty( content ) )
            {
                currentSearchViewModel.ContentsSearchProcessor.Search( content );
                currentSearchViewModel.CurrentSearchProcessorIndex = 0;
            }
        }

        /// <summary>
        /// Searches for the given content in a new tab.
        /// </summary>
        /// <param name="content">Content to search.</param>
        internal void SearchContentInNewTab( string content )
        {
            AddSearch( null );
            SearchContent( content );
        }
    }
}

