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
using System.IO;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// LuceneIndexBuilder class.
    /// 
    /// The LuceneIndexBuilder class implements the IIndexBuilder interface
    /// and uses the Lucene.NET library provided by The Apache Software Foundation for index 
    /// implementation and creation.
    /// </summary>
    /// <seealso cref="IIndexBuilder"/>
    internal class LuceneIndexBuilder : IIndexBuilder, IProgressProvider
    {
        Lucene.Net.Store.Directory mIndex;
        IndexWriter mIndexWriter;
        Analyzer mDefaultAnalyzer;
        Analyzer mDefaultFilePathAnalyzer;
        Analyzer mDefaultFileExtensionAnalyzer;
        Analyzer mDefaultContentsAnalyzer;
        Analyzer mDefaultCaseInsensitiveContentsAnalyzer;
        CodeXCavator.Engine.Tokenizers.TagTokenizer mDefaultTagsTokenizer;
        Analyzer mDefaultTagsAnalyzer;
        Analyzer mDefaultCaseInsensitiveTagsAnalyzer;

        internal const string FIELD_PATH = "Path";
        internal const string FIELD_EXTENSION = "Extension";
        internal const string FIELD_LAST_MODIFIED = "Modified";
        internal const string FIELD_SIZE = "Size";
        internal const string FIELD_CONTENTS = "Contents";
        internal const string FIELD_TAGS = "Tags";
        internal const string FIELD_CASEINSENSITIVE = "CaseInsensitive";
        internal const string FIELD_TAG = "Tag";
        internal const string FIELD_TAG_SOURCE_PATH = "TagSourcePath";
        internal const string FIELD_URL = "Url";
        internal const string FIELD_CAPTION = "Caption";

        internal static readonly CodeXCavator.Engine.Tokenizers.TagTokenizer DEFAULT_TAGS_TOKENIZER = new CodeXCavator.Engine.Tokenizers.TagTokenizer(); 
        internal static readonly Analyzer DEFAULT_FILEPATH_ANALYZER = ( new CodeXCavator.Engine.Tokenizers.WhitespaceSeparatorTokenizer( ':', '/', '\\', '.' ) ).ToAnalyzer( Case.Insensitive );
        internal static readonly Analyzer DEFAULT_FILE_EXTENSION_ANALYZER = ( new CodeXCavator.Engine.Tokenizers.WhitespaceSeparatorTokenizer() ).ToAnalyzer( Case.Insensitive );
        internal static readonly Analyzer DEFAULT_CONTENTS_ANALYZER = ( new CodeXCavator.Engine.Tokenizers.WhitespaceSeparatorTokenizer( TextUtilities.DEFAULT_SEPARATORS ) ).ToAnalyzer();
        internal static readonly Analyzer DEFAULT_TAGS_ANALYZER = ( DEFAULT_TAGS_TOKENIZER ).ToAnalyzer();
        internal static readonly Analyzer DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER = ( new CodeXCavator.Engine.Tokenizers.WhitespaceSeparatorTokenizer( TextUtilities.DEFAULT_SEPARATORS ) ).ToAnalyzer( Case.Insensitive );
        internal static readonly Analyzer DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER = ( new CodeXCavator.Engine.Tokenizers.TagTokenizer() ).ToAnalyzer( Case.Insensitive );

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="index">Index directory.</param>
        /// <param name="overwrite">Specifies, whether the index should be overwritten, or not.</param>
        public LuceneIndexBuilder( Lucene.Net.Store.Directory index, bool overwrite = false )
        {
            IndexPath = index.ToString();
            mIndex = index;

            InitializeDefaultAnalyzers();

            mIndexWriter = new IndexWriter(mIndex, mDefaultAnalyzer, overwrite, IndexWriter.MaxFieldLength.UNLIMITED);
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="indexFilePath">Path to the index root.</param>
        /// <param name="overwrite">Specifies, whether the index should be overwritte, or not.</param>
        public LuceneIndexBuilder( string indexFilePath, bool overwrite = false )
        {
            IndexPath = indexFilePath;
            var indexDirectoryInfo = new System.IO.DirectoryInfo( indexFilePath );
            if( !indexDirectoryInfo.Exists )
                indexDirectoryInfo.Create();

            InitializeDefaultAnalyzers();
            
            mIndex = new SimpleFSDirectory( indexDirectoryInfo, new NativeFSLockFactory() );
            mIndexWriter = new IndexWriter( mIndex, mDefaultAnalyzer, overwrite, IndexWriter.MaxFieldLength.UNLIMITED );
        }

        /// <summary>
        /// Initializes default analyzers for the different index fields
        /// </summary>
        private void InitializeDefaultAnalyzers()
        {
            mDefaultFilePathAnalyzer = DEFAULT_FILEPATH_ANALYZER;
            mDefaultFileExtensionAnalyzer = DEFAULT_FILE_EXTENSION_ANALYZER;
            mDefaultContentsAnalyzer = DEFAULT_CONTENTS_ANALYZER;
            mDefaultTagsTokenizer = DEFAULT_TAGS_TOKENIZER;
            mDefaultTagsAnalyzer = DEFAULT_TAGS_ANALYZER;
            mDefaultCaseInsensitiveContentsAnalyzer = DEFAULT_CASE_INSENSITIVE_CONTENTS_ANALYZER;
            mDefaultCaseInsensitiveTagsAnalyzer = DEFAULT_CASE_INSENSITIVE_TAGS_ANALYZER;

            mDefaultAnalyzer = ConstructDefaultAnalyzer();
        }

        /// <summary>
        /// Constructs the default analyzer.
        /// </summary>
        /// <returns></returns>
        private Analyzer ConstructDefaultAnalyzer()
        {
            return ConstructDefaultAnalyzer
            ( 
                mDefaultFilePathAnalyzer, 
                mDefaultFileExtensionAnalyzer,
                mDefaultContentsAnalyzer, 
                mDefaultCaseInsensitiveContentsAnalyzer, 
                mDefaultTagsAnalyzer, 
                mDefaultCaseInsensitiveTagsAnalyzer 
            );
        }

        private Analyzer ConstructDefaultAnalyzer( ITokenizer contentsTokenizer )
        {
            return ConstructDefaultAnalyzer( contentsTokenizer.ToAnalyzer(), contentsTokenizer.ToAnalyzer( Case.Insensitive ) );
        }

        /// <summary>
        /// Constructs the default analyzer.
        /// </summary>
        /// <returns></returns>
        private Analyzer ConstructDefaultAnalyzer( Analyzer contentsAnalyzer, Analyzer caseInsensitiveContentsAnalyzer )
        {
            return ConstructDefaultAnalyzer
            ( 
                mDefaultFilePathAnalyzer,
                mDefaultFileExtensionAnalyzer,
                contentsAnalyzer, 
                caseInsensitiveContentsAnalyzer, 
                mDefaultTagsAnalyzer, 
                mDefaultCaseInsensitiveTagsAnalyzer 
            );
        }

        private Analyzer ConstructDefaultAnalyzer( ITokenizer contentsTokenizer, ITokenizer tagsTokenizer )
        {
            return ConstructDefaultAnalyzer
            ( 
                mDefaultFilePathAnalyzer,
                mDefaultFileExtensionAnalyzer,
                contentsTokenizer.ToAnalyzer(), 
                contentsTokenizer.ToAnalyzer( Case.Insensitive ), 
                tagsTokenizer.ToAnalyzer(), 
                tagsTokenizer.ToAnalyzer( Case.Insensitive ) 
            );
        }

        /// <summary>
        /// Constructs the default analyzer.
        /// </summary>
        /// <returns></returns>
        private Analyzer ConstructDefaultAnalyzer( Analyzer filePathAnalyzer, Analyzer fileExtensionAnalyzer, Analyzer contentsAnalyzer, Analyzer caseInsensitiveContentsAnalyzer, Analyzer tagsAnalyzer, Analyzer caseInsensitiveTagsAnalyzer )
        {
            return new PerFieldAnalyzerWrapper( mDefaultContentsAnalyzer,
                    new KeyValuePair<string, Analyzer>[] 
                        { 
                            new KeyValuePair<string, Analyzer>( FIELD_PATH, filePathAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_EXTENSION, fileExtensionAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_CONTENTS, contentsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_TAGS, tagsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_CONTENTS + FIELD_CASEINSENSITIVE, caseInsensitiveContentsAnalyzer ),
                            new KeyValuePair<string, Analyzer>( FIELD_TAGS + FIELD_CASEINSENSITIVE, caseInsensitiveTagsAnalyzer )
                        }
                    );
        }

        /// <summary>
        /// Initializes a new document.
        /// </summary>
        /// <param name="filePath">Path of the document.</param>
        /// <param name="fileReader">File reader to be used for reading the document.</param>
        /// <param name="document">Document to be initialized.</param>
        private static void InitializeDocument( Lucene.Net.Documents.Document document, string filePath, System.IO.Stream contentsStream, System.IO.Stream caseInsensitiveContentsStream, System.IO.Stream tagsStream, System.IO.Stream caseInsensitiveTagStream, out System.IO.TextReader contentsReader, out System.IO.TextReader caseInsensitiveContentsReader, out System.IO.TextReader tagsReader, out System.IO.TextReader caseInsensitiveTagsReader )
        {
            contentsReader = new System.IO.StreamReader( contentsStream, Encoding.Default, true );
            tagsReader = new System.IO.StreamReader( tagsStream, Encoding.Default, true );
            caseInsensitiveContentsReader = new System.IO.StreamReader( caseInsensitiveContentsStream, Encoding.Default, true );
            caseInsensitiveTagsReader = new System.IO.StreamReader( caseInsensitiveTagStream, Encoding.Default, true );

            document.Add( new Lucene.Net.Documents.Field( FIELD_PATH, filePath, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED_NO_NORMS ) );
            document.Add( new Lucene.Net.Documents.Field( FIELD_EXTENSION, Path.GetExtension(filePath).ToLower(), Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.ANALYZED_NO_NORMS ) );
            document.Add( new Lucene.Net.Documents.NumericField( FIELD_LAST_MODIFIED ).SetLongValue( FileStorageProviders.GetLastModificationTimeStamp( filePath ) ) );
            document.Add( new Lucene.Net.Documents.NumericField( FIELD_SIZE ).SetLongValue( FileStorageProviders.GetSize( filePath ) ) );
            document.Add( new Lucene.Net.Documents.Field( FIELD_CONTENTS, contentsReader, Lucene.Net.Documents.Field.TermVector.WITH_POSITIONS_OFFSETS ) );
            document.Add( new Lucene.Net.Documents.Field( FIELD_TAGS, tagsReader, Lucene.Net.Documents.Field.TermVector.WITH_POSITIONS_OFFSETS ) );
            document.Add( new Lucene.Net.Documents.Field( FIELD_CONTENTS + FIELD_CASEINSENSITIVE, caseInsensitiveContentsReader, Lucene.Net.Documents.Field.TermVector.WITH_POSITIONS_OFFSETS ) );
            document.Add( new Lucene.Net.Documents.Field( FIELD_TAGS + FIELD_CASEINSENSITIVE, caseInsensitiveTagsReader, Lucene.Net.Documents.Field.TermVector.WITH_POSITIONS_OFFSETS ) );
        }

        private bool GetStreamsForFile( string filePath, out System.IO.Stream contentStream, out System.IO.Stream caseInsensitiveContentStream, out System.IO.Stream tagStream, out System.IO.Stream caseInsensitiveTagStream )
        {
            contentStream = null;
            caseInsensitiveContentStream = null;
            tagStream = null;
            caseInsensitiveTagStream = null;

            System.IO.Stream[] streams = new System.IO.Stream[4];
            for( int i = 0 ; i < 4 ; ++i )
            {
                streams[i] = FileStorageProviders.GetFileStream( filePath );
                if( streams[i] == null )
                {
                    for( int j = 0 ; j < i ; ++j )
                    {
                        if( streams[j] != null )
                            streams[j].Close();
                    } 
                    return false;
                }
            }
            
            contentStream = streams[0];
            tagStream = streams[1];
            caseInsensitiveContentStream = streams[2];
            caseInsensitiveTagStream = streams[3];
            return true;
        }

        public void AddFile( string filePath, ITokenizer tokenizer )
        {
            // Use the file storage provider registry, to open the file for read access.
            System.IO.Stream contentsStream, tagsStream, caseInsensitiveContentsStream, caseInsensitiveTagsStream;
            if( !GetStreamsForFile( filePath, out contentsStream, out caseInsensitiveContentsStream, out tagsStream, out caseInsensitiveTagsStream ) )
            {
                NotifyOnProgress( string.Format( "Error: Opening file \"{0}\" failed!", filePath ) );
                return;
            }

            NotifyOnProgress( string.Format( "Adding file \"{0}\"...", filePath ) );

            var tagTokenList = new List<IToken>();
            Action<IToken> tagHandler = ( tag ) => tagTokenList.Add( tag );
            mDefaultTagsTokenizer.OnTag += tagHandler;

            using( contentsStream )
            using( tagsStream )
            using( caseInsensitiveContentsStream )
            using( caseInsensitiveTagsStream )
            {
                try
                {
                    System.IO.TextReader contentReader;
                    System.IO.TextReader tagsReader;
                    System.IO.TextReader caseInsensitiveContentReader;
                    System.IO.TextReader caseInsensitiveTagsReader;
                    var document = new Lucene.Net.Documents.Document();
                    InitializeDocument( document, filePath, contentsStream, caseInsensitiveContentsStream, tagsStream, caseInsensitiveTagsStream, out contentReader, out caseInsensitiveContentReader, out tagsReader, out caseInsensitiveTagsReader );

                    if( tokenizer == null )                    
                    {
                        // Use default tokenizer...
                        mIndexWriter.AddDocument( document );
                    }
                    else
                    {
                        // Use provided tokenizer...
                        var analyzer = ConstructDefaultAnalyzer( tokenizer );
                        mIndexWriter.AddDocument( document, analyzer );
                    }

                    contentsStream.Close();
                    tagsStream.Close();

                    // Add tag documents
                    if (tagTokenList.Count > 0)
                        AddTagDocuments(filePath, tagTokenList);
                }
                catch
                {
                }
            }

            mDefaultTagsTokenizer.OnTag -= tagHandler;
        }

        /// <summary>
        /// Adds for each tag token a tag document to the index.
        /// 
        /// A tag document is a special document, which carries tag information.
        /// </summary>
        /// <param name="tagTokenList">List of tag tokens.</param>
        private void AddTagDocuments( string filePath, List<IToken> tagTokenList )
        {
            if( tagTokenList == null )
                return;

            foreach( var tagToken in tagTokenList )
            {
                if( tagToken.Data != null )
                {
                    CodeXCavator.Engine.Tokenizers.TagTokenizer.TagInfo[] tagInfos = tagToken.Data as CodeXCavator.Engine.Tokenizers.TagTokenizer.TagInfo[];
                    if( tagInfos != null )
                    {
                        foreach( var tagInfo in tagInfos )
                        {
                            var document = new Lucene.Net.Documents.Document();
                            InitializeTagDocument( document, filePath, tagToken.Text, tagInfo );
                            mIndexWriter.AddDocument( document );                            
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a tag document.
        /// </summary>
        /// <param name="document">Document, which should be initialized.</param>
        /// <param name="tagSourceFilePath">Path to the source file, which contains the tag.</param>
        /// <param name="tag">Tag</param>
        /// <param name="tagInfo">Tag info structure.</param>
        private void InitializeTagDocument( Lucene.Net.Documents.Document document, string tagSourceFilePath, string tag, CodeXCavator.Engine.Tokenizers.TagTokenizer.TagInfo tagInfo )
        {
            document.Add( new Lucene.Net.Documents.Field( FIELD_TAG, tag, Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.NOT_ANALYZED_NO_NORMS, Lucene.Net.Documents.Field.TermVector.NO ) );
            document.Add( new Lucene.Net.Documents.Field( FIELD_TAG_SOURCE_PATH, tagSourceFilePath, Lucene.Net.Documents.Field.Store.NO, Lucene.Net.Documents.Field.Index.NOT_ANALYZED_NO_NORMS, Lucene.Net.Documents.Field.TermVector.NO ) );
            if( tagInfo.Url != null )
                document.Add( new Lucene.Net.Documents.Field( FIELD_URL, tagInfo.Url, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.NO ) );
            if( tagInfo.Caption != null )
                document.Add( new Lucene.Net.Documents.Field( FIELD_CAPTION, tagInfo.Caption, Lucene.Net.Documents.Field.Store.YES, Lucene.Net.Documents.Field.Index.NO ) );
        }

        public void UpdateFile( string filePath, ITokenizer tokenizer )
        {
            // Use the file storage provider registry, to open the file for read access.
            System.IO.Stream contentsStream, tagsStream, caseInsensitiveContentsStream, caseInsensitiveTagsStream;
            if( !GetStreamsForFile( filePath, out contentsStream, out caseInsensitiveContentsStream, out tagsStream, out caseInsensitiveTagsStream ) )
            {
                NotifyOnProgress( string.Format( "Error: Opening file \"{0}\" failed!", filePath ) );
                return;
            }

            NotifyOnProgress( string.Format( "Updating file \"{0}\"...", filePath ) );

            var tagTokenList = new List<IToken>();
            Action<IToken> tagHandler = ( tag ) => tagTokenList.Add( tag );
            mDefaultTagsTokenizer.OnTag += tagHandler;

            using( contentsStream )
            using( tagsStream )
            {
                try
                {
                    System.IO.TextReader contentReader;
                    System.IO.TextReader tagsReader;
                    System.IO.TextReader caseInsensitiveContentReader;
                    System.IO.TextReader caseInsensitiveTagsReader;
                    var document = new Lucene.Net.Documents.Document();
                    InitializeDocument( document, filePath, contentsStream, caseInsensitiveContentsStream, tagsStream, caseInsensitiveTagsStream, out contentReader, out caseInsensitiveContentReader, out tagsReader, out caseInsensitiveTagsReader );

                    if( tokenizer == null )
                    {
                        // Use default tokenizer...
                        mIndexWriter.UpdateDocument( new Term( FIELD_PATH, filePath ), document );
                    }
                    else
                    {
                        // Use provided tokenizer...
                        var analyzer = ConstructDefaultAnalyzer( tokenizer );
                        mIndexWriter.UpdateDocument( new Term( FIELD_PATH, filePath ), document, analyzer );
                    }

                    contentsStream.Close();
                    tagsStream.Close();

                    // Remove existing tag documents for the current file
                    mIndexWriter.DeleteDocuments( new Term( FIELD_TAG_SOURCE_PATH, filePath ) );
                    // Add new tag documents
                    if (tagTokenList.Count > 0)
                        AddTagDocuments(filePath, tagTokenList);
                }
                catch
                {
                }
            }

            mDefaultTagsTokenizer.OnTag -= tagHandler;
        }

        public void RemoveFile( string filePath )
        {
            NotifyOnProgress( string.Format( "Removing file \"{0}\"...", filePath ) );
            mIndexWriter.DeleteDocuments( new Term( FIELD_PATH, filePath ) );
        }

        public void Clear()
        {
            NotifyOnProgress( "Clearing index..." );
            mIndexWriter.DeleteAll();
        }

        public void Dispose()
        {
            if( mIndexWriter != null )
            {
                mIndexWriter.Dispose();
                mIndexWriter = null;
            }
            if( mIndex != null )
            {
                mIndex.Dispose();
                mIndex = null;
            }
        }

        protected void NotifyOnProgress( string progressMessage )
        {
            if( OnProgress != null )
                OnProgress( this, progressMessage );
        }

        public event Action<object, string> OnProgress;

        public string IndexPath
        {
            get;
            private set;
        }
    }
}
