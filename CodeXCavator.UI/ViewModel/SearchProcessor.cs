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

//This file has been modified by Ben van der Merwe

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using CodeXCavator.Engine.Interfaces;
using System.Windows.Input;
using System.Collections;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// SearchProcessor class.
    /// 
    /// The search processor class processes a search query in the background asynchronously
    /// and returns the search results.
    /// </summary>
    public class SearchProcessor : ViewModelBase
    {
        private string mSearchType;
        private bool mCountMatches;
        private bool mUseFileTypeFilter;
        private bool mUseDirectoryFilter;
        private BackgroundWorker mSearchWorker;
        private BackgroundWorker mSearchHitCountWorker;

        /// <summary>
        /// Search type
        /// </summary>
        internal string SearchType { get { return mSearchType; } }

        /// <summary>
        /// Index view model of the index on which the search is performed.
        /// </summary>
        internal IndexViewModel Index { get; set; }

        private IOptionsProvider mSearchOptions;
        private IIndexSearcher mSearcher;

        /// <summary>
        /// Searcher to be used for processing the search query.
        /// </summary>
        internal IIndexSearcher Searcher 
        { 
            get { return mSearcher; } 
            set
            {
                if( mSearchOptions != null )
                    mSearchOptions.OptionChanged -= OnSearchOptionChanged;
                mSearcher = value;
                var optionsProvider = mSearcher as IOptionsProvider;
                mSearchOptions = optionsProvider != null ? new OptionsProviderWrapper( optionsProvider ) : null;                
                if( mSearchOptions != null )
                {
                    mSearchOptions.OptionChanged -= OnSearchOptionChanged;
                    mSearchOptions.OptionChanged += OnSearchOptionChanged;
                }
                OnPropertyChanged( "SearchOptions" );
            }
        }

        private FileTypeFilter mFileTypeFilter;

        /// <summary>
        /// File type filter, which limits the search results to specific file types.
        /// </summary>
        internal FileTypeFilter FileTypeFilter
        {
            get { return mFileTypeFilter; }
            set
            {
                if( mFileTypeFilter != null )
                    mFileTypeFilter.PropertyChanged -= FilterCriteriaChanged;
                mFileTypeFilter = value;
                if( mFileTypeFilter != null )
                    mFileTypeFilter.PropertyChanged += FilterCriteriaChanged;
            }
        }

        DirectoryFilter mDirectoryFilter;

        /// <summary>
        /// Directory filter, which limits the search results to certain directories.
        /// </summary>
        internal DirectoryFilter DirectoryFilter
        {
            get { return mDirectoryFilter; }
            set
            {
                if( mDirectoryFilter != null )
                    mDirectoryFilter.PropertyChanged -= FilterCriteriaChanged;
                mDirectoryFilter = value;
                if( mDirectoryFilter != null )
                    mDirectoryFilter.PropertyChanged += FilterCriteriaChanged;
            }
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="searchType">Type of the search.</param>
        /// <param name="countMatches">Determines, whether the total number of matches should be counted.</param>
        /// <param name="useFileTypeFilter">Determines, whether the file type filter should be applied.</param>
        /// <param name="useDirectoryFilter">Determines, whether the directory filter should be applied.</param>
        public SearchProcessor( string searchType, bool countMatches, bool useFileTypeFilter, bool useDirectoryFilter )
        {
            mSearchType = searchType;
            mCountMatches = countMatches;
            mUseDirectoryFilter = useDirectoryFilter;
            mUseFileTypeFilter = useFileTypeFilter;
            mImmediateSearch = false;
        }

        private bool mRequery;
        private string mSearchQuery;
        private System.Windows.Threading.DispatcherTimer mSearchDelayTimer;
        private string mResultsQuery;

        /// <summary>
        /// Query phrase, which was used to determine the search results.
        /// 
        /// Do not mix it up with the SearchQuery property.
        /// </summary>
        public string SearchResultsQuery
        {
            get
            {
                return mResultsQuery;
            }

            set
            {
                if( mResultsQuery != value )
                {
                    mResultsQuery = value;
                    OnPropertyChanged( "SearchResultsQuery" );
                }
            }
        }

        private bool mIsSearching;

        /// <summary>
        /// Signalizes, whether a search is currently running, or not.
        /// </summary>
        public bool IsSearching
        {
            get
            {
                return mIsSearching;
            }
            set
            {
                if( mIsSearching != value )
                {
                    if( value )
                        SearchResults = null;
                    mIsSearching = value;
                    OnPropertyChanged( "IsSearching" );
                }
            }
        }

        /// <summary>
        /// Current search query phrase.
        /// </summary>
        public string SearchQuery
        {
            get
            {
                return mSearchQuery;
            }
            set
            {
                if( mSearchQuery != value )
                {
                    mSearchQuery = value;

                    if( string.IsNullOrEmpty( mSearchQuery ) )
                    {
                        DetachFromSearchBackgroundWorkers();
                        OnPropertyChanged( "SearchQuery" );
                        OnPropertyChanged( "HasSearchQuery" );
                        SearchResults = null;
                        SearchResultCount = null;
                        SearchResultsQuery = null;
                        TotalMatchCount = null;
                        IsSearching = false;
                        IsCountingMatches = false;
                        return;
                    }

                    // Validate search query...
                    if( Searcher != null && !string.IsNullOrEmpty( mSearchQuery ) )
                    {
                        if( !Searcher.IsValidSearchQuery( mSearchQuery ) )
                            throw new ArgumentException( string.Format( "Invalid search query: {0}", mSearchQuery ) );
                    }

                    if( !mIsSearching )
                    {
                        if( mImmediateSearch )
                        {
                            // Trigger search delay timer. This will delay the search, until the user stops typing.
                            InitializeSearchDelayTimer( ref mSearchDelayTimer, OnSearchDelayExpired );
                        }
                    }
                    else
                    {
                        // Indicate that after the search query has finished it should be requeried again.
                        mRequery = true;
                    }
                    OnPropertyChanged( "SearchQuery" );
                    OnPropertyChanged( "HasSearchQuery" );
                }
            }
        }

        public bool HasSearchQuery
        {
            get { return !string.IsNullOrEmpty(mSearchQuery); }
        }

        /// <summary>
        /// Returns the options of the current search.
        /// </summary>
        public IOptionsProvider SearchOptions { get { return mSearchOptions; } }

        public void SetSearchOption(string option, object value)
        {
            mSearchOptions.SetOptionValue(option, value);
        }

        private bool mImmediateSearch;

        /// <summary>
        /// Determines, whether search is performed immediately or not.
        /// </summary>
        public bool ImmediateSearch
        {
            get { return mImmediateSearch; }
            set 
            {
                if( mImmediateSearch != value )
                {
                    mImmediateSearch = value;
                    if( mImmediateSearch )
                        StartSearch( mSearchType, mSearchQuery, mUseFileTypeFilter ? FileTypeFilter.AllowedFileTypes : null, mUseDirectoryFilter ? DirectoryFilter.AllowedDirectories : null );
                    OnPropertyChanged( "ImmediateSearch" );
                }
            }
        }

        private IEnumerable mSearchResults;

        /// <summary>
        /// Enumeration of search result view models.
        /// </summary>
        public IEnumerable SearchResults
        {
            get
            {
                return mSearchResults;
            }
            set
            {
                if( mSearchResults != value )
                {
                    mSearchResults = value;
                    OnPropertyChanged( "SearchResults" );
                }
            }
        }

        int? mSearchResultCount;

        /// <summary>
        /// Returns the number of search results, i.e. the number of files which matched the search query.
        /// 
        /// This is intentionally Nullable. If the value is null the UI displays an in-progress
        /// indicator, signalizing the counting is in progress.
        /// </summary>
        public int? SearchResultCount
        {
            get { return mSearchResultCount; }
            set
            {
                if( mSearchResultCount != value )
                {
                    mSearchResultCount = value;
                    OnPropertyChanged( "SearchResultCount" );
                }
            }
        }

        int? mTotalMatchCount;

        /// <summary>
        /// Returns the total number of matches.
        /// 
        /// This is intentionally Nullable. If the value is null the UI displays an in-progress
        /// indicator, signalizing the counting is in progress.
        /// </summary>
        public int? TotalMatchCount
        {
            get { return mTotalMatchCount; }
            set
            {
                if( mTotalMatchCount != value )
                {
                    mTotalMatchCount = value;
                    OnPropertyChanged( "TotalMatchCount" );
                }
            }
        }

        private bool mIsCountingMatches;

        /// <summary>
        /// Signalizes, whether the total number of matches is beeing counted.
        /// </summary>
        public bool IsCountingMatches
        {
            get
            {
                return mIsCountingMatches;
            }
            set
            {
                if( mIsCountingMatches != value )
                {
                    mIsCountingMatches = value;
                    OnPropertyChanged( "IsCountingMatches" );
                }
            }
        }

        private int mProgress;

        /// <summary>
        /// Progress of counting total number of matches.
        /// </summary>
        public int Progress
        {
            get
            {
                return mProgress;
            }
        }

        /// <summary>
        /// Progress of counting total number of matches scaled to 0.0-1.0.
        /// </summary>
        public double UnitProgress
        {
            get
            {
                return (double) mProgress / 100.0;
            }
        }

        /// <summary>
        /// Initializes a dispatcher timer, which will delay the search
        /// until the timer expires.
        /// </summary>
        internal static void InitializeSearchDelayTimer( ref System.Windows.Threading.DispatcherTimer searchTimer, EventHandler timerHandler )
        {
            if( searchTimer == null )
            {
                searchTimer = new System.Windows.Threading.DispatcherTimer( new TimeSpan( 0, 0, 1 ), System.Windows.Threading.DispatcherPriority.ApplicationIdle, timerHandler, System.Windows.Threading.Dispatcher.CurrentDispatcher );
            }
            searchTimer.Start();
        }

        /// <summary>
        /// Handles change of search options.
        /// </summary>
        /// <param name="optionsProvider">Options provider, whose option has changed.</param>
        /// <param name="option">Option, which has changed.</param>
        void OnSearchOptionChanged( IOptionsProvider optionsProvider, IOption option )
        {
            if( ImmediateSearch )
                StartSearch( mSearchType, mSearchQuery, mUseFileTypeFilter ? FileTypeFilter.AllowedFileTypes : null, mUseDirectoryFilter ? DirectoryFilter.AllowedDirectories : null );            
            OnPropertyChanged( "SearchOptions." + option.Name );
        }

        /// <summary>
        /// Handles the expiration of the search delay timer.
        /// 
        /// This method actually triggers the search.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        void OnSearchDelayExpired( object sender, EventArgs e )
        {
            mSearchDelayTimer.Stop();
            StartSearch( mSearchType, mSearchQuery, mUseFileTypeFilter ? FileTypeFilter.AllowedFileTypes : null, mUseDirectoryFilter ? DirectoryFilter.AllowedDirectories : null );
        }

        /// <summary>
        /// Processes search results and creates a SearchHitViewModel for each search result.
        /// </summary>
        /// <param name="searchResults">Search results, for which view models should be created.</param>
        /// <returns></returns>
        private static IEnumerable CreateViewModelsForSearchResults( IEnumerable<ISearchHit> searchResults, ObservableEnumerable parent )
        {
            if( searchResults != null )
            {
                foreach( var searchResult in searchResults )
                {
                    SearchHitViewModel viewModel = ViewModel.SearchHitViewModel.GetOrCreateSearchHitViewModel( searchResult, parent );
                    yield return viewModel;
                    if( viewModel.IsExpanded )
                        foreach( var occurrence in viewModel.Occurrences )
                            yield return occurrence;
                }
            }
            yield break;
        }

        /// <summary>
        /// Performs search.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void searchWorker_DoWork( object sender, DoWorkEventArgs e )
        {
            var input = (Tuple<IIndexSearcher, string, string, string[], Tuple<string, bool, bool>[]>) e.Argument;
            try
            {
                // Trigger a search on the index searcher.
                int searchResultCount;
                var searchResults = input.Item1.Search( input.Item2, input.Item3, out searchResultCount, input.Item4, input.Item5 );
                // Gather search hits.
                e.Result = new Tuple<IEnumerable<ISearchHit>, int, string>( searchResults, searchResultCount, input.Item3 );
            }
            catch
            {
                e.Result = null;
            }
        }

        /// <summary>
        /// Handles search completion.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void searchWorker_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            IsSearching = false;
            if( e.Result != null )
            {
                var result = (Tuple<IEnumerable<ISearchHit>, int, string>) e.Result;

                if( result.Item2 > 0 )
                {
                    // Start background worker, which will count the total number of matches
                    if( !mRequery )
                    {
                        if( mCountMatches )
                        {
                            IsCountingMatches = true;
                            if( mSearchHitCountWorker != null )
                            {
                                mSearchHitCountWorker.CancelAsync();
                                DetachFromSearchHitCountBackgroundWorker();
                            }
                            mSearchHitCountWorker = new BackgroundWorker();
                            mSearchHitCountWorker.WorkerReportsProgress = true;
                            mSearchHitCountWorker.WorkerSupportsCancellation = true;
                            mSearchHitCountWorker.DoWork += searchHitCounter_DoWork;
                            mSearchHitCountWorker.RunWorkerCompleted += searchHitCounter_RunWorkerCompleted;
                            mSearchHitCountWorker.ProgressChanged += searchHitCounter_ProgressChanged;
                            mSearchHitCountWorker.RunWorkerAsync( result.Item1 );
                        }
                    }
                    var resultsEnumerable = new ObservableEnumerable();
                    resultsEnumerable.Enumerable = CreateViewModelsForSearchResults( result.Item1, resultsEnumerable );
                    SearchResults = resultsEnumerable;
                }
                else
                {
                    SearchResults = new SearchHitViewModel[] {};
                    TotalMatchCount = 0;
                }

                // Set results.
                SearchResultCount = result.Item2;
                SearchResultsQuery = result.Item3;
            }
            else
            {
                // Set results.
                SearchResultCount = 0;
                TotalMatchCount = 0;
                SearchResults = null;
            }

            // Restart search if requested.
            if( mRequery )
            {

                mRequery = false;
                StartSearch( mSearchType, mSearchQuery, mUseFileTypeFilter ? FileTypeFilter.AllowedFileTypes : null, mUseDirectoryFilter ? DirectoryFilter.AllowedDirectories : null );
            }
        }

        /// <summary>
        /// Counts total number of search hits.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void searchHitCounter_DoWork( object sender, DoWorkEventArgs e )
        {
            IEnumerable<ISearchHit> searchHits = (IEnumerable<ISearchHit>) e.Argument;
            int searchHitCount = searchHits.Count();
            int totalMatchCount = 0;
            // Iterate over document search hits.
            int current = 0;
            foreach( var searchHit in searchHits )
            {
                if( e.Cancel )
                {
                    totalMatchCount = 0;
                    break;
                }
                // Count match occurrences for each document and sum it up.
                var occurrences = searchHit.Occurrences;
                totalMatchCount += occurrences != null ? occurrences.Count : 0;
                ++current;
                ( (BackgroundWorker) sender ).ReportProgress( ( current * 100 ) / searchHitCount );
            }
            e.Result = totalMatchCount;
        }

        /// <summary>
        /// Handles progress change of the search hit counter background worker.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        void searchHitCounter_ProgressChanged( object sender, ProgressChangedEventArgs e )
        {
            mProgress = e.ProgressPercentage;
            OnPropertyChanged( "Progress" );
            OnPropertyChanged( "UnitProgress" );
        }

        /// <summary>
        /// Handles completion of counting search query match occurrences..
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void searchHitCounter_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            if( e.Cancelled )
                return;
            TotalMatchCount = (int) e.Result;
            IsCountingMatches = false;
        }

        /// <summary>
        /// Starts the search.
        /// 
        /// Search is performed asynchronously using a background worker.
        /// </summary>
        /// <param name="searchType">Search type.</param>
        /// <param name="searchQuery">Search query.</param>
        internal void StartSearch( string searchType, string searchQuery )
        {
            StartSearch( searchType, searchQuery, mUseFileTypeFilter ? FileTypeFilter.AllowedFileTypes : null, mUseDirectoryFilter ? DirectoryFilter.AllowedDirectories : null );
        }

        /// <summary>
        /// Starts the search.
        /// 
        /// Search is performed asynchronously using a background worker.
        /// </summary>
        /// <param name="searchType">Search type.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <param name="allowedFileTypes">Allowed file types.</param>
        /// <param name="directories">Allowed directories.</param>
        internal void StartSearch( string searchType, string searchQuery, string[] allowedFileTypes, Tuple<string, bool, bool>[] directories )
        {
            IsSearching = !( mSearchQuery == null || ( mSearchQuery.Trim().Length == 0 ) );
            if( !IsSearching )
                return;

            SearchResultCount = null;
            TotalMatchCount = null;
            SearchResultsQuery = null;

            DetachFromSearchBackgroundWorkers();

            // Start a background worker, which will perform the search.
            mSearchWorker = new BackgroundWorker();
            mSearchWorker.WorkerSupportsCancellation = false;
            mSearchWorker.WorkerReportsProgress = false;
            mSearchWorker.DoWork += searchWorker_DoWork;
            mSearchWorker.RunWorkerCompleted += searchWorker_RunWorkerCompleted;
            mSearchWorker.RunWorkerAsync( new Tuple<IIndexSearcher, string, string, string[], Tuple<string, bool, bool>[]>( Index.Index.CreateSearcher( SearchOptions ), searchType, searchQuery, allowedFileTypes, directories ) );
        }

        /// <summary>
        /// Detaches from running search background workers.
        /// </summary>
        private void DetachFromSearchBackgroundWorkers()
        {
            DetachFromSearchBackgroundWorker();
            DetachFromSearchHitCountBackgroundWorker();
        }

        /// <summary>
        /// Detaches from search worker
        /// </summary>
        private void DetachFromSearchBackgroundWorker()
        {
            if( mSearchWorker != null )
            {
                mSearchWorker.DoWork -= searchWorker_DoWork;
                mSearchWorker.RunWorkerCompleted -= searchWorker_RunWorkerCompleted;
            }
        }

        /// <summary>
        /// Detaches from search hit cout worker
        /// </summary>
        private void DetachFromSearchHitCountBackgroundWorker()
        {
            if( mSearchHitCountWorker != null )
            {
                mSearchHitCountWorker.DoWork -= searchHitCounter_DoWork;
                mSearchHitCountWorker.RunWorkerCompleted -= searchHitCounter_RunWorkerCompleted;
                mSearchHitCountWorker.ProgressChanged -= searchHitCounter_ProgressChanged;
            }
        }

        /// <summary>
        /// Handles the change of filter criteria.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void FilterCriteriaChanged( object sender, PropertyChangedEventArgs e )
        {
            if( !mIsSearching )
            {
                // Trigger search delay timer.
                // This will deley search, until the user stops modifying the filter criteria.
                InitializeSearchDelayTimer( ref mSearchDelayTimer, OnSearchDelayExpired );
            }
            else
            {
                // Indicate that after the search query has finished it should be requeried again.
                mRequery = true;
            }
        }

        private ICommand mSearchCommand;

        /// <summary>
        /// Search command.
        /// </summary>
        public ICommand SearchCommand
        {
            get
            {
                if( mSearchCommand == null )
                    mSearchCommand = new Command( CanSearch, Search );
                return mSearchCommand;
            }
        }

        /// <summary>
        /// Determines, whether search can be performed.
        /// </summary>
        /// <param name="param">Command parameter.</param>
        /// <returns>True, if search can be performed, false otherwise.</returns>
        private bool CanSearch( object param )
        {
            return !mImmediateSearch;
        }

        /// <summary>
        /// Performs search.
        /// </summary>
        /// <param name="param">Command parameter.</param>
        private void Search( object param )
        {
            StartSearch( mSearchType, mSearchQuery, mUseFileTypeFilter ? FileTypeFilter.AllowedFileTypes : null, mUseDirectoryFilter ? DirectoryFilter.AllowedDirectories : null );
        }

        /// <summary>
        /// Searches for the specified search terms.
        /// </summary>
        /// <param name="searchQuery">Search query.</param>
        public void Search( string searchQuery )
        {
            if( mSearchQuery != searchQuery )
            {
                mSearchQuery = searchQuery;
                OnPropertyChanged( "SearchQuery" );
                OnPropertyChanged( "HasSearchQuery" );
            }
            StartSearch( mSearchType, mSearchQuery, mUseFileTypeFilter ? FileTypeFilter.AllowedFileTypes : null, mUseDirectoryFilter ? DirectoryFilter.AllowedDirectories : null );
        }

        private ICommand mClearSearchQueryCommand;

        /// <summary>
        /// Clear search query command.
        /// </summary>
        public ICommand ClearSearchQueryCommand
        {
            get
            {
                if( mClearSearchQueryCommand == null )
                    mClearSearchQueryCommand = new Command( CanClearSearchQuery, ClearSearchQuery );
                return mClearSearchQueryCommand;
            }
        }

        /// <summary>
        /// Determines, whether the search query can be cleared.
        /// </summary>
        /// <param name="arg">Command parameter.</param>
        /// <returns>True, if a search query is set, false otherwise.</returns>
        private bool CanClearSearchQuery(object arg)
        {
            return HasSearchQuery;
        }

        /// <summary>
        /// Clears the search query.
        /// </summary>
        /// <param name="arg">Command parameter.</param>
        private void ClearSearchQuery(object arg)
        {
            SearchQuery = null;
        }
    }

}
