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
using CodeXCavator.Engine.Interfaces;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Documents;
using System.Collections.ObjectModel;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// Occurrence class.
    /// 
    /// Implements the IOccurrence interface and stores occurrence information.
    /// </summary>
    /// <seealso cref="IOccurrence"/>
    internal class Occurrence : IOccurrence
    {
        internal Occurrence( string match, int lineNumber, int column, KeyValuePair<string,int>[] fragment )
        {
            Match = match;
            Line = lineNumber;
            Column = column;
            Fragment = fragment;
        }


        public int Line
        {
            get;
            private set;
        }

        public int Column
        {
            get;
            private set;
        }

        public string Match
        {
            get;
            private set;
        }

        public KeyValuePair<string, int>[] Fragment
        {
            get;
            private set;
        }

        public override bool Equals( object obj )
        {
            if( ReferenceEquals( this, obj ) )
                return true;
            var otherOccurrence = obj as IOccurrence;
            if( otherOccurrence == null )
                return false;
            if( otherOccurrence.Column != Column )
                return false;
            if( otherOccurrence.Line != Line )
                return false;
            if( otherOccurrence.Match != Match )
                return false;
            return true;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Match != null ? Match.GetHashCode() : 0;
                hashCode = ( hashCode * 131 ) ^ Line;
                hashCode = ( hashCode * 131 ) ^ Column;
                return hashCode;
            }
        }
    }

    /// <summary>
    /// LuceneSearchHit class.
    /// 
    /// Implements the ISearchHit interface. Uses Lucene highlighter to create information about match occurrences.
    /// </summary>
    /// <seealso cref="ISearchHit"/>
    internal class LuceneSearchHit : ISearchHit
    {
        Query mQuery;
        bool mCaseSensitive;
        // TODO: Split into two different search hit classes ( one for normal content search hits, and one for file search hits )
        string mSearchType;
        Document mDocument;
        Analyzer mDefaultFilePathAnalyzer = LuceneIndexBuilder.DEFAULT_FILEPATH_ANALYZER;
        Analyzer mDefaultContentsAnalyzer = LuceneIndexBuilder.DEFAULT_CONTENTS_ANALYZER;
        Analyzer mDefaultTagsAnalyzer = LuceneIndexBuilder.DEFAULT_TAGS_ANALYZER;
        Analyzer mDefaultCaseInsensitiveContentsAnalyzer = LuceneIndexBuilder.DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER;
        Analyzer mDefaultCaseInsensitiveTagsAnalyzer = LuceneIndexBuilder.DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER;
        ReadOnlyCollection<IOccurrence> mOccurrences;

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="query">Query, which was matched by the search hit.</param>
        /// <param name="document">Lucene document, which satisifed the query.</param>
        /// <param name="score">Scoring of the document.</param>
        public LuceneSearchHit( Query query, Document document, double score, bool caseSensitive, string searchType = SearchType.Contents )
        {
            mDocument = document;
            mSearchType = searchType;
            mQuery = query;
            mCaseSensitive = caseSensitive;
            Score = score;
        }

        public string FilePath
        {
            get
            {
                return mDocument.Get( LuceneIndexBuilder.FIELD_PATH );
            }
        }

        public double Score
        {
            get;
            private set;
        }


        public System.Collections.ObjectModel.ReadOnlyCollection<IOccurrence> Occurrences
        {
            get 
            {
                if( mOccurrences == null )
                    mOccurrences = DetermineOccurrences();
                return mOccurrences;                
            }
        }

        protected virtual Analyzer FilePathAnalyzer
        {
            get { return mDefaultFilePathAnalyzer; }
        }

        protected virtual Analyzer ContentsAnalyzer
        {
            get { return mCaseSensitive ? mDefaultContentsAnalyzer : mDefaultCaseInsensitiveContentsAnalyzer; }
        }

        protected virtual Analyzer TagsAnalyzer
        {
            get { return mCaseSensitive ? mDefaultTagsAnalyzer : mDefaultCaseInsensitiveTagsAnalyzer; }
        }

        /// <summary>
        /// Determines a list of search query matches in the document.
        /// </summary>
        /// <returns>List of IOccurrence instances.</returns>
        private ReadOnlyCollection<IOccurrence> DetermineOccurrences()
        {
            // This must be thread-safe as this method is called from a background worker.
            System.Threading.Monitor.Enter( this );
            try
            {
                return LuceneSearchUtilities.DetermineOccurrences( mSearchType, FilePath, mQuery, FilePathAnalyzer, ContentsAnalyzer, TagsAnalyzer );
            }
            catch
            {
                return null;
            }
            finally
            {
                // This must be thread-safe as this method is called from a background worker.
                System.Threading.Monitor.Exit( this );
            }
        }

        /// <summary>
        /// Line scanner state.
        /// </summary>
        private enum LineScannerState
        {
            /// <summary>
            /// Initial state.
            /// </summary>
            Init, 
            /// <summary>
            /// Line break.
            /// </summary>
            LineBreak,
        }
    }

    /// <summary>
    /// LuceneIndexSearcher class.
    /// 
    /// The LuceneIndexSearcher class implementes the IIndexSearcher interface
    /// and uses the Lucene.NET library provided by The Apache Software Foundation for index 
    /// search.
    /// </summary>
    /// <seealso cref="IIndexSearcher"/>
    public class LuceneIndexSearcher : IIndexSearcher
    {
        public static readonly QueryParser DEFAULT_CONTENT_PARSER = new QueryParser( Lucene.Net.Util.Version.LUCENE_30, LuceneIndexBuilder.FIELD_CONTENTS, LuceneIndexBuilder.DEFAULT_CONTENTS_ANALYZER ) { AllowLeadingWildcard = true, LowercaseExpandedTerms = false };

        Directory mIndex;
        IndexReader mIndexReader;
        Analyzer mDefaultFilePathAnalyzer;
        Analyzer mDefaultContentsAnalyzer;
        Analyzer mDefaultTagsAnalyzer;
        Analyzer mDefaultCaseInsensitiveContentsAnalyzer;
        Analyzer mDefaultCaseInsensitiveTagsAnalyzer;
        Analyzer mDefaultAnalyzer;
        QueryParser mFilePathQueryParser;
        QueryParser mContentsQueryParser;
        QueryParser mTagsQueryParser;
        QueryParser mCaseInsensitiveContentsQueryParser;
        QueryParser mCaseInsensitiveTagsQueryParser;
        IndexSearcher mSearcher;

        protected virtual QueryParser FilePathQueryParser
        {
            get { return mFilePathQueryParser; }
        }

        protected virtual QueryParser ContentsQueryParser
        {
            get { return CaseSensitive ? mContentsQueryParser : mCaseInsensitiveContentsQueryParser; }
        }

        protected virtual QueryParser TagsQueryParser
        {
            get { return CaseSensitive ? mTagsQueryParser : mCaseInsensitiveTagsQueryParser; }
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="index">Index directory.</param>
        public LuceneIndexSearcher( Directory index ) 
        {
            Initialize( index );
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="indexFilePath">File path to the index root.</param>
        public LuceneIndexSearcher( string indexFilePath )
        {
            Initialize( new SimpleFSDirectory( new System.IO.DirectoryInfo( indexFilePath ), new NativeFSLockFactory() ) );
        }

        /// <summary>
        /// Applies the given options.
        /// </summary>
        /// <param name="options">Options, which should be applie.</param>
        public void ApplyOptions( IOptionsProvider options )
        {
            if( options == null )
                return;
            foreach( var option in Options )
            {
                try
                {
                    var optionValue = options.GetOptionValue( option.Name );
                    if( optionValue != null )
                        SetOptionValue( option, optionValue );
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Initializes the index searcher.
        /// </summary>
        /// <param name="index"></param>
        protected void Initialize( Directory index )
        {
            mIndex = index;
            mDefaultFilePathAnalyzer = LuceneIndexBuilder.DEFAULT_FILEPATH_ANALYZER;
            mDefaultContentsAnalyzer = LuceneIndexBuilder.DEFAULT_CONTENTS_ANALYZER;
            mDefaultTagsAnalyzer = LuceneIndexBuilder.DEFAULT_TAGS_ANALYZER;
            mDefaultCaseInsensitiveContentsAnalyzer = LuceneIndexBuilder.DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER;
            mDefaultCaseInsensitiveTagsAnalyzer = LuceneIndexBuilder.DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER;
            mDefaultAnalyzer = new PerFieldAnalyzerWrapper( mDefaultContentsAnalyzer,
                    new KeyValuePair<string, Analyzer>[] 
                        { 
                            new KeyValuePair<string, Analyzer>( LuceneIndex.FIELD_PATH, mDefaultFilePathAnalyzer ),
                            new KeyValuePair<string, Analyzer>( LuceneIndex.FIELD_CONTENTS, mDefaultContentsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( LuceneIndex.FIELD_TAGS, mDefaultTagsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( LuceneIndex.FIELD_CONTENTS + LuceneIndex.FIELD_CASEINSENSITIVE, mDefaultCaseInsensitiveContentsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( LuceneIndex.FIELD_TAGS + LuceneIndex.FIELD_CASEINSENSITIVE, mDefaultCaseInsensitiveTagsAnalyzer )
                        }
                    );

            // We want wildcard matching with leading wildcards 
            // and we also do always want case-sensitive search.            
            mIndexReader                        = IndexReader.Open( mIndex, true );
            mFilePathQueryParser                = new QueryParser( Lucene.Net.Util.Version.LUCENE_30, LuceneIndexBuilder.FIELD_PATH, mDefaultFilePathAnalyzer ) { AllowLeadingWildcard = true, LowercaseExpandedTerms = true };
            mContentsQueryParser                = new QueryParser( Lucene.Net.Util.Version.LUCENE_30, LuceneIndexBuilder.FIELD_CONTENTS, mDefaultContentsAnalyzer ) { AllowLeadingWildcard = true, LowercaseExpandedTerms = false };
            mTagsQueryParser                    = new QueryParser( Lucene.Net.Util.Version.LUCENE_30, LuceneIndexBuilder.FIELD_TAGS, mDefaultContentsAnalyzer ) { AllowLeadingWildcard = true, LowercaseExpandedTerms = false };
            mCaseInsensitiveContentsQueryParser = new QueryParser( Lucene.Net.Util.Version.LUCENE_30, LuceneIndexBuilder.FIELD_CONTENTS + LuceneIndexBuilder.FIELD_CASEINSENSITIVE, mDefaultCaseInsensitiveContentsAnalyzer ) { AllowLeadingWildcard = true, LowercaseExpandedTerms = true };
            mCaseInsensitiveTagsQueryParser     = new QueryParser( Lucene.Net.Util.Version.LUCENE_30, LuceneIndexBuilder.FIELD_TAGS + LuceneIndexBuilder.FIELD_CASEINSENSITIVE, mDefaultCaseInsensitiveContentsAnalyzer ) { AllowLeadingWildcard = true, LowercaseExpandedTerms = true };
                        
            mSearcher = new IndexSearcher( mIndexReader );

            CaseSensitive = true;
        }

        public IEnumerable<ISearchHit> Search( string searchType, string searchQuery )
        {
            int unusedHitCount;
            return Search( searchType, searchQuery, out unusedHitCount );
        }

        public IEnumerable<ISearchHit> Search( string searchType, string searchQuery, out int numHits )
        {
            var query = SearchQuery( searchType, searchQuery );
            if( query == null )
            {
                numHits = 0;
                return null;
            }
            var resultsCollector = TopScoreDocCollector.Create( mIndexReader.NumDocs(), true );
            mSearcher.Search( query, resultsCollector );
            numHits = resultsCollector.TotalHits;
            return SearchResults( searchType, query, resultsCollector ).ToArray();
        }

        public IEnumerable<ISearchHit> Search( string searchType, string searchQuery, out int numHits, params string[] allowedFileTypes )
        {
            var query = SearchQuery( searchType, searchQuery );
            if( query == null )
            {
                numHits = 0;
                return null;
            }
            var resultsCollector = TopScoreDocCollector.Create( mIndexReader.NumDocs(), true );
            mSearcher.Search( query, new FileFilter( allowedFileTypes ), resultsCollector );
            numHits = resultsCollector.TotalHits;
            return SearchResults( searchType, query, resultsCollector ).ToArray();
        }

        public IEnumerable<ISearchHit> Search( string searchType, string searchQuery, out int numHits, string[] allowedFileTypes, Tuple<string, bool, bool>[] directories )
        {
            var query = SearchQuery( searchType, searchQuery );
            if( query == null )
            {
                numHits = 0;
                return null;
            }
            var resultsCollector = TopScoreDocCollector.Create( mIndexReader.NumDocs(), true );
            mSearcher.Search( query, new FileFilter( allowedFileTypes, directories ), resultsCollector );
            numHits = resultsCollector.TotalHits;
            return SearchResults( searchType, query, resultsCollector ).ToArray();
        }

        /// <summary>
        /// Returns a search query, for the specified search type
        /// </summary>
        /// <param name="searchType">Search type.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>Query object, or null, if search type is not supported.</returns>
        private Query SearchQuery( string searchType, string searchQuery  )
        {
            
            switch( searchType )
            {
                case SearchType.Files:
                    return FilePathQueryParser.Parse( searchQuery );
                    
                case SearchType.Contents:
                    return ContentsQueryParser.Parse( searchQuery );

                case SearchType.Tags:
                    return TagsQueryParser.Parse( searchQuery );

                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns search results.
        /// </summary>
        /// <param name="searchType">Search type.</param>
        /// <param name="query">Search query.</param>
        /// <param name="resultsCollector">Results collector containing search results.</param>
        /// <returns>Search results enumeration, or null if search type is not supported.</returns>
        private IEnumerable<ISearchHit> SearchResults( string searchType, Query query, TopScoreDocCollector resultsCollector )
        {
            switch( searchType )
            {
                case SearchType.Files:
                    return FileSearchResults( query, resultsCollector );
                case SearchType.Contents:
                    return ContentsSearchResults( query, resultsCollector );
                case SearchType.Tags:
                    return TagsSearchResults( query, resultsCollector );
                default:
                    return null;
            }
        }

        /// <summary>
        /// Returns contents search results.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="resultsCollector">Results collector containing search results.</param>
        /// <returns>Search results enumeration, or null if search type is not supported.</returns>
        private IEnumerable<ISearchHit> ContentsSearchResults( Query query, TopScoreDocCollector resultsCollector )
        {
            foreach( var doc in resultsCollector.TopDocs().ScoreDocs )
            {
                if( FileStorageProviders.Exists( mSearcher.Doc( doc.Doc ).GetField( LuceneIndexBuilder.FIELD_PATH ).StringValue ) )
                    yield return new LuceneSearchHit( query, mSearcher.Doc( doc.Doc ), doc.Score, CaseSensitive, SearchType.Contents );
            }
        }

        /// <summary>
        /// Returns tags search results.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="resultsCollector">Results collector containing search results.</param>
        /// <returns>Search results enumeration, or null if search type is not supported.</returns>
        private IEnumerable<ISearchHit> TagsSearchResults( Query query, TopScoreDocCollector resultsCollector )
        {
            foreach( var doc in resultsCollector.TopDocs().ScoreDocs )
            {
                if( FileStorageProviders.Exists( mSearcher.Doc( doc.Doc ).GetField( LuceneIndexBuilder.FIELD_PATH ).StringValue ) )
                    yield return new LuceneSearchHit( query, mSearcher.Doc( doc.Doc ), doc.Score, CaseSensitive, SearchType.Tags );
            }
        }

        /// <summary>
        /// Returns file search results.
        /// </summary>
        /// <param name="query">Search query.</param>
        /// <param name="resultsCollector">Results collector containing search results.</param>
        /// <returns>Search results enumeration, or null if search type is not supported.</returns>
        private IEnumerable<ISearchHit> FileSearchResults( Query query, TopScoreDocCollector resultsCollector )
        {
            foreach( var doc in resultsCollector.TopDocs().ScoreDocs )
            {
                yield return new LuceneSearchHit( query, mSearcher.Doc( doc.Doc ), doc.Score, CaseSensitive, SearchType.Files );
            }
        }
        
        public bool IsValidSearchQuery( string searchTerm )
        {
            try
            {
                var query = ContentsQueryParser.Parse( searchTerm );
                return query != null;
            }
            catch
            {
            }
            return false;
        }

        public bool IsValidFileSearchQuery( string searchTerm )
        {
            try
            {
                var query = FilePathQueryParser.Parse( searchTerm );
                return query != null;
            }
            catch
            {
            }
            return false;
        }

        public void Dispose()
        {
            if( mIndexReader != null )
            {
                mIndexReader.Dispose();
                mIndexReader = null;
            }

            if( mIndex != null )
            {
                mIndex.Dispose();
                mIndex = null;
            }
        }

        /// <summary>
        /// FileFilter class.
        /// 
        /// The FileFilter class dervices from the Lucene.Net.Search.Filter abstract class and
        /// implements a file type and directory filter.
        /// 
        /// It is used to return matches only in files of a certain file type and 
        /// only in files, which are located below a certain directory.
        /// </summary>
        private class FileFilter : Lucene.Net.Search.Filter
        {
            private string[] mAllowedFileTypes;
            private Tuple<string, bool, bool>[] mDirectories;

            /// <summary>
            /// Initialization constructor.
            /// </summary>
            /// <param name="allowedFileTypes">File types, to which search should be limited.</param>
            public FileFilter( string[] allowedFileTypes )
            {
                mAllowedFileTypes = allowedFileTypes;
            }

            /// <summary>
            /// Initialization constructor.
            /// </summary>
            /// <param name="allowedFileTypes">File types, to which search should be limited.</param>
            /// <param name="directories">Directories, to which search should be limited. 
            /// The first element of the tuple specifies a directory wildcard pattern, 
            /// the second element determines, whether the directory should be treated recursively, 
            /// the third element determines, whether files contained in the directory, should be excluded from search (true), or included (false).</param>
            public FileFilter( string[] allowedFileTypes, Tuple<string, bool, bool>[] directories )
            {
                mAllowedFileTypes = allowedFileTypes;
                mDirectories = directories;
            }

            /// <summary>
            /// FileExtensionFilterDocIdSet class.
            /// 
            /// The FileExtensionFilterDocIdSet class derives from the abstract DocIdSet class.
            /// 
            /// It provides a set of document ids matching the file filter.
            /// </summary>
            private class FileFilterDocIdSet : DocIdSet
            {
                FileFilter mParentFilter;
                IndexReader mIndexReader;

                /// <summary>
                /// Initialization constructor.
                /// </summary>
                /// <param name="parentFilter">File filter, the id set belongs to.</param>
                /// <param name="indexReader">Index reader, providing access to the index, which should be filtered.</param>
                public FileFilterDocIdSet( FileFilter parentFilter, IndexReader indexReader )
                {
                    mParentFilter = parentFilter;
                    mIndexReader = indexReader;
                }

                /// <summary>
                /// FileFilterDocIdSetIterator class.
                /// 
                /// The class FileFilterDocIdSetIterator implements the abstract DocIdSetIterator class and
                /// allows iterating over the set of document ids filtered by the file filter.
                /// </summary>
                private class FileFilterDocIdSetIterator : DocIdSetIterator
                {
                    private HashSet<string> mAllowedFileTypes;
                    private Tuple<string, bool, bool>[] mDirectories;
                    IndexReader mIndexReader;
                    int mDocIndex = -1;
                    int mDocCount;

                    /// <summary>
                    /// Initialization constructor.
                    /// </summary>
                    /// <param name="parentFilter">File filter, the iterator belongs to.</param>
                    /// <param name="indexReader">Index reader, providing access to the index, which should be filtered.</param>
                    public FileFilterDocIdSetIterator( FileFilter parentFilter, IndexReader indexReader )
                    {
                        mAllowedFileTypes = parentFilter.mAllowedFileTypes != null ? new HashSet<string>( parentFilter.mAllowedFileTypes, StringComparer.OrdinalIgnoreCase ) : null;
                        mDirectories = parentFilter.mDirectories;
                        mIndexReader = indexReader;
                        mDocCount = indexReader.NumDocs();
                    }

                    public override int Advance( int target )
                    {
                        int doc;
                        for( doc = NextDoc() ; doc < target ; doc = NextDoc() ) ;
                        return doc;
                    }

                    public override int DocID()
                    {
                        if( mDocIndex < 0 )
                            return -1;

                        if( mDocIndex >= mDocCount )
                            return NO_MORE_DOCS;

                        return mDocIndex;
                    }

                    public override int NextDoc()
                    {
                        var pathSelector = new Lucene.Net.Documents.MapFieldSelector( LuceneIndexBuilder.FIELD_PATH );
                        while( mDocIndex < mDocCount - 1 )
                        {
                            ++mDocIndex;
                            // Skip deleted files.
                            if( !mIndexReader.IsDeleted( mDocIndex ) )
                            {
                                var document = mIndexReader.Document( mDocIndex, pathSelector );
                                var fieldPath = document.GetField( LuceneIndexBuilder.FIELD_PATH );
                                if( fieldPath != null )
                                {
                                    string path = fieldPath.StringValue;
                                    // Return only, if matches file extension and directory filter
                                    if( MatchesExtensionFilter( path ) && MatchesDirectoryFilter( path ) )
                                    {
                                        return mDocIndex;
                                    }
                                }
                            }
                        }
                        return NO_MORE_DOCS;
                    }

                    /// <summary>
                    /// Checks, whether the path matches the file extension filter.
                    /// </summary>
                    /// <param name="path">Path, which should be checked.</param>
                    /// <returns>True, if the path matches the file extension filter, false otherwise.</returns>
                    private bool MatchesExtensionFilter( string path )
                    {
                        string extension = System.IO.Path.GetExtension( path );
                        return ( mAllowedFileTypes == null || mAllowedFileTypes.Contains( extension ) );
                    }

                    /// <summary>
                    /// Checks, whether the path matches the directory filter.
                    /// </summary>
                    /// <param name="path">Path, which should be checked.</param>
                    /// <returns>True, if the path matches the directory filter, false otherwise.</returns>
                    private bool MatchesDirectoryFilter( string path )
                    {
                        if( mDirectories == null || mDirectories.Length == 0 )
                            return true;
                        bool inclusive = !mDirectories.Any( directory => !directory.Item3 );
                        bool exclusive = false;
                        // Process all directories in the directory filter.
                        foreach( var directory in mDirectories )
                        {
                            // Normalize path pattern
                            string normalizedPattern = directory.Item1.Trim();
                            normalizedPattern = NormalizeDirectoryPattern( normalizedPattern, directory.Item2 );

                            // Check, whether the path matches the directory pattern.
                            string directoryOfPath = System.IO.Path.GetDirectoryName( path ) + "\\";
                            bool matches = ( Microsoft.VisualBasic.CompilerServices.Operators.LikeString( directoryOfPath, normalizedPattern, Microsoft.VisualBasic.CompareMethod.Text ) );
                            // Update inclusive and exclusive flag according to match result
                            if( matches )
                            {
                                if( directory.Item3 )
                                    exclusive |= true;
                                else
                                    inclusive |= true;
                            }
                        }
                        // Return intersection of both sets: [only included files] ^ [all files except excluded]
                        return (inclusive && !exclusive);
                    }

                    /// <summary>
                    /// Normalizes a directory pattern.
                    /// </summary>
                    /// <param name="directoryPattern">Directory wildcard pattern.</param>
                    /// <param name="recursive">Determines, whether the directory pattern sould match recursively or not.</param>
                    /// <returns>Normalized directory pattern.</returns>
                    private static string NormalizeDirectoryPattern( string directoryPattern, bool recursive )
                    {
                        var lastCharInPattern = !string.IsNullOrEmpty( directoryPattern ) ? directoryPattern.Last() : '\0';
                        if( lastCharInPattern != '\\' && lastCharInPattern != '/' )
                            directoryPattern += recursive ? "\\*" : "\\";
                        else
                            if( recursive && lastCharInPattern != '*' )
                                directoryPattern += '*';
                        return directoryPattern;
                    }
                }

                public override DocIdSetIterator Iterator()
                {
                    return new FileFilterDocIdSetIterator( mParentFilter, mIndexReader );
                }
            }

            public override DocIdSet GetDocIdSet( IndexReader reader )
            {
                var allowedDocuments = new FileFilterDocIdSet( this, reader );
                return allowedDocuments;
            }
        }

        /// <summary>
        /// List of supported search options.
        /// </summary>
        public static readonly IOption[] SupportedSearchOptions = { Interfaces.SearchOptions.CaseSensitive };

        public IEnumerable<IOption> Options
        {
            get { return SupportedSearchOptions; }
        }

        public void SetOptionValue( IOption option, object value )
        {
            if( option == Interfaces.SearchOptions.CaseSensitive )
                CaseSensitive = Convert.ToBoolean( value );
        }

        public object GetOptionValue( IOption option )
        {
            if( option == Interfaces.SearchOptions.CaseSensitive )
                return CaseSensitive;
            return null;
        }

        private bool mCaseSensitive;

        /// <summary>
        /// Controls, whether search should be case sensitive or not.
        /// </summary>
        public bool CaseSensitive 
        { 
            get { return mCaseSensitive; }
            set
            {
                if( mCaseSensitive != value )
                {
                    mCaseSensitive = value;
                    NotifyOptionChanged( Interfaces.SearchOptions.CaseSensitive );
                }
            }
        }

        public event OptionChangedEvent OptionChanged;

        protected virtual void NotifyOptionChanged( IOption option )
        {
            if( OptionChanged != null )
                OptionChanged( this, option );
        }
    }

    /// <summary>
    /// Lucene related search utility functions
    /// </summary>
    public static class LuceneSearchUtilities
    {
        /// <summary>
        /// OccurrenceCollectorFormatter class.
        /// 
        /// This class implements Lucene.Net.Search.Highlight.IFormatter. It does not really
        /// format the incoming text, but creates a list of Occurrence objects for each
        /// element, which should be highlighted.
        /// </summary>
        private class OccurrenceCollectorFormatter : Lucene.Net.Search.Highlight.IFormatter
        {
            public IList<KeyValuePair<string, Tuple<int, int, int, int>>> Occurrences = new List<KeyValuePair<string, Tuple<int, int, int, int>>>();
            public string HighlightTerm( string originalText, Lucene.Net.Search.Highlight.TokenGroup tokenGroup )
            {
                if( tokenGroup.TotalScore > 0.0 )
                    Occurrences.Add( new KeyValuePair<string, Tuple<int, int, int, int>>( originalText, new Tuple<int, int, int, int>( tokenGroup.StartOffset, tokenGroup.EndOffset, tokenGroup.MatchStartOffset, tokenGroup.MatchEndOffset ) ) );
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines a list of search query matches.
        /// </summary>
        /// <param name="searchType">Type of search.</param>
        /// <param name="filePath">File path of the document, which should be searched.</param>
        /// <param name="query">Query, whose matches should be returned.</param>
        /// <param name="filesAnalyzer">Files analyzer to be used.</param>
        /// <param name="contentsAnalyzer">Contents analyzer to be used.</param>
        /// <param name="tagsAnalyzer">Tags analyzer to be used.</param>
        /// <returns>Collection of search query match occurrences.</returns>
        public static ReadOnlyCollection<IOccurrence> DetermineOccurrences( string searchType, string filePath, Query query, Analyzer filesAnalyzer, Analyzer contentsAnalyzer, Analyzer tagsAnalyzer )
        {
            try
            {
                string originalText = null;
                bool createFragment = true;
                switch( searchType )
                {
                    case SearchType.Files:
                        {
                            createFragment = false;
                            originalText = filePath;
                        }
                        break;
                    default:
                        {
                            createFragment = true;
                            // Use file storage provider registry to get read only access to the original file.
                            using( var fileStream = FileStorageProviders.GetFileStream( filePath ) )
                            {
                                if( fileStream != null )
                                {
                                    using( var reader = new System.IO.StreamReader( fileStream, Encoding.Default, true ) )
                                    {
                                        // Read the whole text.
                                        originalText = reader.ReadToEnd();
                                        reader.Close();
                                    }
                                }
                                else
                                {
                                    return null;
                                }
                            }
                        }
                        break;
                }

                return DetermineOccurrences( searchType, query, originalText, filesAnalyzer, contentsAnalyzer, tagsAnalyzer, createFragment );
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines a list of search query matches.
        /// </summary>
        /// <param name="searchType">Type of search.</param>
        /// <param name="query">Query, whose matches should be returned.</param>
        /// <param name="filesAnalyzer">Files analyzer to be used.</param>
        /// <param name="contentsAnalyzer">Contents analyzer to be used.</param>
        /// <param name="tagsAnalyzer">Tags analyzer to be used.</param>
        /// <param name="text">Original text</param>
        /// <param name="createFragment">Determines, whether fragments should be created or not.</param>
        /// <returns>Collection of search query match occurrences.</returns>
        public static ReadOnlyCollection<IOccurrence> DetermineOccurrences( string searchType, Query query, string text, Analyzer filesAnalyzer, Analyzer contentsAnalyzer, Analyzer tagsAnalyzer, bool createFragment )
        {
            // Create and configure a Lucene highlighter in order to obtain a list of search 
            // query matches in the document.
            var scorer = new Lucene.Net.Search.Highlight.QueryScorer( query );
            var occurrenceCollector = new OccurrenceCollectorFormatter();
            var highlighter = new Lucene.Net.Search.Highlight.Highlighter( occurrenceCollector, scorer );
            // We must extend the analyzer limit to the whole text.
            highlighter.MaxDocCharsToAnalyze = text.Length;
            // We want to analyze the whole text, thus the NullFragmenter.
            highlighter.TextFragmenter = new Lucene.Net.Search.Highlight.NullFragmenter();
            switch( searchType )
            {
                case SearchType.Files:
                    highlighter.GetBestFragments( filesAnalyzer.ReusableTokenStream( LuceneIndexBuilder.FIELD_PATH, new System.IO.StringReader( text ) ), text, 1 );
                    break;
                case SearchType.Contents:
                    highlighter.GetBestFragments( contentsAnalyzer.ReusableTokenStream( LuceneIndexBuilder.FIELD_CONTENTS, new System.IO.StringReader( text ) ), text, 1 );
                    break;
                case SearchType.Tags:
                    highlighter.GetBestFragments( tagsAnalyzer.ReusableTokenStream( LuceneIndexBuilder.FIELD_TAGS, new System.IO.StringReader( text ) ), text, 1 );
                    break;
            }

            // Obtain line offsets from original file. This is needed in order to initialize the 
            // Line and Column properts of IOccurrence.
            int[] lineOffsets = TextUtilities.GetLineOffsets( text ).ToArray();

            // Process the list of matches obtained through the Lucene highlighter
            // and create a list of IOccurrenc instances.
            List<IOccurrence> occurrences = new List<IOccurrence>();
            foreach( var occurrence in occurrenceCollector.Occurrences )
            {
                // Obtain line number from offset by doing binary search.
                int line = TextUtilities.OffsetToLineIndex( occurrence.Value.Item3, lineOffsets );
                // Get start offset of line, this is needed to obtain the right column number.
                int lineOffset = lineOffsets[line];

                List<KeyValuePair<string, int>> fragment = null;
                // Create occurrence fragment. Currently the fragment consists of 6 lines around the match.
                if( createFragment )
                {
                    int fragmentStartLine = Math.Max( line - 2, 0 );
                    int fragmentEndLine = Math.Min( fragmentStartLine + 3, lineOffsets.Length - 2 );
                    fragment = new List<KeyValuePair<string, int>>();
                    for( int i = fragmentStartLine ; i < fragmentEndLine ; ++i )
                        fragment.Add( new KeyValuePair<string, int>( text.Substring( lineOffsets[i], lineOffsets[i + 1] - lineOffsets[i] ), i ) );
                }

                // Add occurrence
                occurrences.Add( new Occurrence( occurrence.Key.Substring( occurrence.Value.Item3 - occurrence.Value.Item1, occurrence.Value.Item4 - occurrence.Value.Item3 ), line, occurrence.Value.Item3 - lineOffset, fragment != null ? fragment.ToArray() : null ) );
            }

            return new ReadOnlyCollection<IOccurrence>( occurrences );
        }

    }
}
