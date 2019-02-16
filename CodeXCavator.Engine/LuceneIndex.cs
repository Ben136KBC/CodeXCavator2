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
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Index;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// LuceneIndex class.
    /// 
    /// The LuceneIndex class implements the IIndex interface 
    /// and uses the Lucene.NET library provided by The Apache Software Foundation for index 
    /// implementation.
    /// </summary>
    /// <seealso cref="IIndex"/>
    internal class LuceneIndex : IIndex
    {
        Directory mIndex;
        IndexReader mIndexReader;
        Analyzer mDefaultAnalyzer;
        Analyzer mDefaultFilePathAnalyzer;
        Analyzer mDefaultContentsAnalyzer;
        Analyzer mDefaultTagsAnalyzer;
        Analyzer mDefaultCaseInsensitiveContentsAnalyzer;
        Analyzer mDefaultCaseInsensitiveTagsAnalyzer;
        
        internal const string FIELD_PATH = LuceneIndexBuilder.FIELD_PATH;
        internal const string FIELD_LAST_MODIFIED = LuceneIndexBuilder.FIELD_LAST_MODIFIED;
        internal const string FIELD_SIZE = LuceneIndexBuilder.FIELD_SIZE;
        internal const string FIELD_CONTENTS = LuceneIndexBuilder.FIELD_CONTENTS;
        internal const string FIELD_TAGS = LuceneIndexBuilder.FIELD_TAGS;
        internal const string FIELD_TAG = LuceneIndexBuilder.FIELD_TAG;
        internal const string FIELD_TAG_SOURCE_PATH = LuceneIndexBuilder.FIELD_TAG_SOURCE_PATH;
        internal const string FIELD_URL = LuceneIndexBuilder.FIELD_URL;
        internal const string FIELD_CAPTION = LuceneIndexBuilder.FIELD_CAPTION;
        internal const string FIELD_CASEINSENSITIVE = LuceneIndexBuilder.FIELD_CASEINSENSITIVE;

        internal static readonly Analyzer DEFAULT_FILEPATH_ANALYZER = LuceneIndexBuilder.DEFAULT_FILEPATH_ANALYZER;
        internal static readonly Analyzer DEFAULT_CONTENTS_ANALYZER = LuceneIndexBuilder.DEFAULT_CONTENTS_ANALYZER;
        internal static readonly Analyzer DEFAULT_TAGS_ANALYZER = LuceneIndexBuilder.DEFAULT_TAGS_ANALYZER;
        internal static readonly Analyzer DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER = LuceneIndexBuilder.DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER;
        internal static readonly Analyzer DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER = LuceneIndexBuilder.DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER;

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="index">Index directory.</param>
        /// <param name="overwrite">Specifies, whether the index should be overwritten, or not.</param>
        public LuceneIndex( Directory index, bool overwrite = false )
        {
            IndexPath = index.ToString();
            mIndex = index;
            mIndexReader = IndexReader.Open( mIndex, true );

            mDefaultFilePathAnalyzer = DEFAULT_FILEPATH_ANALYZER;
            mDefaultContentsAnalyzer = DEFAULT_CONTENTS_ANALYZER;
            mDefaultTagsAnalyzer = DEFAULT_TAGS_ANALYZER;
            mDefaultCaseInsensitiveContentsAnalyzer = DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER;
            mDefaultCaseInsensitiveTagsAnalyzer = DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER;

            mDefaultAnalyzer = ConstructDefaultAnalyzer();
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="indexFilePath">Path to the index root.</param>
        /// <param name="overwrite">Specifies, whether the index should be overwritte, or not.</param>
        public LuceneIndex( string indexFilePath, bool overwrite = false )
        {
            IndexPath = indexFilePath;
            var indexDirectoryInfo = new System.IO.DirectoryInfo( indexFilePath );
            if( !indexDirectoryInfo.Exists )
                indexDirectoryInfo.Create();

            mDefaultFilePathAnalyzer = DEFAULT_FILEPATH_ANALYZER;
            mDefaultContentsAnalyzer = DEFAULT_CONTENTS_ANALYZER;
            mDefaultTagsAnalyzer = DEFAULT_TAGS_ANALYZER;
            mDefaultCaseInsensitiveContentsAnalyzer = DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER;
            mDefaultCaseInsensitiveTagsAnalyzer = DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER;

            mDefaultAnalyzer = ConstructDefaultAnalyzer();

            mIndex = new SimpleFSDirectory( indexDirectoryInfo, new NativeFSLockFactory() );
            mIndexReader = IndexReader.Open( mIndex, true );
        }

        /// <summary>
        /// Creates the default analyzer for the index.
        /// </summary>
        /// <returns></returns>
        private Analyzer ConstructDefaultAnalyzer()
        {
            return new PerFieldAnalyzerWrapper( mDefaultContentsAnalyzer,
                    new KeyValuePair<string, Analyzer>[] 
                        { 
                            new KeyValuePair<string, Analyzer>( FIELD_PATH, mDefaultFilePathAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_CONTENTS, mDefaultContentsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_TAGS, mDefaultTagsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_CONTENTS + FIELD_CASEINSENSITIVE, mDefaultCaseInsensitiveContentsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_TAGS + FIELD_CASEINSENSITIVE, mDefaultCaseInsensitiveTagsAnalyzer )
                        }
                    );
        }

        public IEnumerable<string> Files
        {
            get 
            {
                var pathSelector = new Lucene.Net.Documents.MapFieldSelector( FIELD_PATH );
                int numDocs = mIndexReader.NumDocs();
                for( int i = 0 ; i < numDocs ; ++i )
                {
                    if( !mIndexReader.IsDeleted( i ) )
                    {
                        var document = mIndexReader.Document( i, pathSelector );
                        var pathField = document.GetField( FIELD_PATH );
                        if( pathField != null )
                            yield return pathField.StringValue;
                    }
                }
            }
        }

        public IEnumerable<TagInfo> Tags
        {
            get
            {
                var tags = mIndexReader.Terms( new Term( FIELD_TAGS ) );
                if( tags != null )
                {
                    do
                    {
                        if( tags.Term != null && string.Equals( tags.Term.Field, FIELD_TAGS ) )
                            yield return new TagInfo { Tag = tags.Term.Text, TotalCount = ComputeTotalTagCount( tags.Term.Text ), DocumentCount = mIndexReader.DocFreq( new Term( FIELD_TAGS, tags.Term.Text ) ), Links = GetTagLinks( tags.Term.Text ).Distinct() };
                    }
                    while( tags.Next() );
                }
            }
        }

        public TagInfo GetTagInfo( string tag )
        {
            if( !string.IsNullOrEmpty( tag ) )
            {
                int totalTagCount = ComputeTotalTagCount( tag );
                int documentCount = mIndexReader.DocFreq( new Term( FIELD_TAGS, tag) );
                var links = GetTagLinks( tag ).Distinct();
                if( totalTagCount > 0 )
                {
                    return new TagInfo { Tag = tag, TotalCount = totalTagCount, DocumentCount = documentCount, Links = links };
                }
            }
            return null;
        }

        private int ComputeTotalTagCount( string tag )
        {
            int count = 0;
            int numDocs = mIndexReader.NumDocs();
            for( int i = 0 ; i < numDocs ; ++i )
            {
                if( !mIndexReader.IsDeleted( i ) )
                {
                    var termFreqVec = mIndexReader.GetTermFreqVector( i, FIELD_TAGS );
                    if( termFreqVec != null )
                    {
                        var tagIndex = termFreqVec.IndexOf( tag );
                        if( tagIndex >= 0 )
                            count += termFreqVec.GetTermFrequencies()[tagIndex];
                    }
                }
            }

            return count;
        }

        private IEnumerable<LinkInfo> GetTagLinks( string tag )
        {
            var tagDocs = mIndexReader.TermDocs( new Term( FIELD_TAG, tag ) );
            var urlAndCaptionSelector = new Lucene.Net.Documents.MapFieldSelector( FIELD_URL, FIELD_CAPTION );
            while( tagDocs.Next() )
            {
                var tagDocument = mIndexReader.Document( tagDocs.Doc, urlAndCaptionSelector );
                if( tagDocument != null )
                {
                    var fieldUrl = tagDocument.GetField( FIELD_URL );
                    var fieldCaption = tagDocument.GetField( FIELD_CAPTION );
                    if( fieldUrl != null && !string.IsNullOrEmpty( fieldUrl.StringValue ) )
                    {
                        yield return new
                            LinkInfo
                            {
                                Url = fieldUrl.StringValue,
                                Caption = fieldCaption != null ? fieldCaption.StringValue : string.Empty
                            };
                    }
                }
            }
            yield break;
        }

        public IEnumerable<string> FileTypes
        {
            get { return Files.Select( file => System.IO.Path.GetExtension( file ) ).Distinct( StringComparer.OrdinalIgnoreCase ); }
        }

        public IIndexSearcher CreateSearcher()
        {
            return new LuceneIndexSearcher( mIndex );
        }

        public IIndexSearcher CreateSearcher( IOptionsProvider searchOptions )
        {
            var searcher = new LuceneIndexSearcher( mIndex );
            searcher.ApplyOptions( searchOptions );
            return searcher;
        }

        public IIndexBuilder CreateBuilder( bool overwrite = false )
        {
            return new LuceneIndexBuilder( mIndex, overwrite );
        }

        public string IndexPath
        {
            get;
            private set;
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
    }
}
