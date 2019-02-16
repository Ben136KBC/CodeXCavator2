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
using System.IO;
using System.Xml.Linq;
using CodeXCavator.Engine.Enumerators;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Xml.Serialization;

namespace CodeXCavator.Engine.Interfaces
{
    /// <summary>
    /// IFileStorageProvider interface.
    /// 
    /// A file storage provider is responsible for providing read access to files.
    /// 
    /// This interface can be used to add support for version control systems, i.e.
    /// such that files can be read directly from them without the necessity of copying
    /// them locally.
    /// </summary>
    public interface IFileStorageProvider
    {
        /// <summary>
        /// Returns, whether the specified file path is supported by the provider.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        bool SupportsPath( string filePath );
        /// <summary>
        /// Opens the specified file and returns a stream, which can be used to read the contents
        /// of the file.
        /// </summary>
        /// <param name="filePath">Path of the file, which should be opened for reading.</param>
        /// <returns>Stream to the contents of the specified file, or null, if the file does not exist or the provider does not support the specified file path.</returns>
        Stream GetFileStream( string filePath );
        /// <summary>
        /// Returns a timestamp of the last modification of the specified file.
        /// </summary>
        /// <param name="filePath">Path of the file, for which a modification time stamp should be returned by the provider.</param>
        /// <returns>Time stamp of last modification of the specified file.</returns>
        long GetLastModificationTimeStamp( string filePath );
        /// <summary>
        /// Returns the size of the specified file in bytes.
        /// </summary>
        /// <param name="filePath">Path of the file, whose size should be retrieved.</param>
        /// <returns>Size of the specified file in bytes.</returns>
        long GetSize( string filePath );
        /// <summary>
        /// Checks, whether the specified file exists.
        /// </summary>
        /// <param name="filePath">Path of the file, whose existence should be checked.</param>
        /// <returns>True, if the file exists, false otherwise.</returns>
        bool Exists( string filePath );
    }

    /// <summary>
    /// IFileEnumerator interface.
    /// 
    /// A file enumerator is responsible for enumerating absolute file paths.
    /// </summary>
    public interface IFileEnumerator : IEnumerable< string >
    {        
        /// <summary>
        /// Enumerates files.
        /// </summary>
        /// <returns>Enumerable of absolute file paths.</returns>
        IEnumerable<string> EnumerateFiles();
    }

    /// <summary>
    /// IFileCatalogueEnumerator interface.
    /// 
    /// A file catalogue enumerator is a file enumerator, which enumerates the contents of a file catalogue.
    /// The catalogue may be enumerated recursively.
    /// 
    /// Besides simple directory enumeration the IFileCatalogueEnumerator interface can be used to provide
    /// support for project or solution files, which allows to enumerate only source files associated with a certain
    /// project.
    /// </summary>
    public interface IFileCatalogueEnumerator : IFileEnumerator
    {
        /// <summary>
        /// Path of the file catalogue, which should be enumerated.
        /// </summary>
        string FileCataloguePath { get; set; }
        /// <summary>
        /// Specifies, whether the file catalogue should be enumerated recursively or not.
        /// This may be ignored, if it does not make sense for a certain implementation.
        /// </summary>
        bool Recursive { get; set; }
        /// <summary>
        /// Checks, whether the specified file catalogue is supported by the enumerator.
        /// </summary>
        /// <param name="fileCataloguePath">Path to the file catalogue, which should be checked.</param>
        /// <returns>True, if the file catalogue enumerator does support enumerating the specified file catalogue, false otherwise.</returns>
        bool SupportsPath( string fileCataloguePath );

        /// <summary>
        /// Enumerates files contained in the specified file catalogue.
        /// </summary>
        /// <param name="fileCataloguePath">Path to the file catalogue, which should be enumerated.</param>
        /// <param name="recursive">Indicates, whether the file catalogue should be enumerated recursively or not.</param>
        /// <returns>Enumerable of files contained in the catalogue, or null, if the file catalogue enumerator does not support enumerating the file catalogue.</returns>
        IEnumerable<string> EnumerateFiles( string fileCataloguePath, bool recursive = false );
    }

    /// <summary>
    /// File filter mode.
    /// </summary>
    public enum FileFilterMode
    {
        /// <summary>
        /// The filter includes all files matching certain criteria.
        /// </summary>
        Inclusive,
        /// <summary>
        /// The filter excludes all files matching certain criteria.
        /// </summary>
        Exclusive
    }

    /// <summary>
    /// IFileFilter interface.
    /// 
    /// A file filter is responsible for filtering certian files out depending on it's configuration.
    /// </summary>
    public interface IFileFilter
    {
        /// <summary>
        /// Filters the specified files according to the filter configuration.
        /// </summary>
        /// <param name="files">Enumeration of files</param>
        /// <returns>Filtered enumeration of files.</returns>
        IEnumerable<string> Filter( IEnumerable< string > files );
    }

    /// <summary>
    /// IInvertibleFileFilter interface.
    /// 
    /// An invertible file filter can operate in two modes "Inclusive" or "Exclusive". 
    /// Depending on the mode it either filters certain files out, or it passes certain files thru according to it's configuration.
    /// </summary>
    public interface IInvertibleFileFilter : IFileFilter
    {
        /// <summary>
        /// Mode of the filter.
        /// 
        /// This mode is applied when calling IFileFilter.Filter().
        /// </summary>
        FileFilterMode Mode { get; set; }
        /// <summary>
        /// Filters the specified files according to the filter configuration and the specified mode.
        /// </summary>
        /// <param name="files">Enumeration of files.</param>
        /// <param name="mode">Mode of the filter.</param>
        /// <returns>Filtered enumeration of files.</returns>
        IEnumerable<string> Filter( IEnumerable<string> files, FileFilterMode mode );
    }

    /// <summary>
    /// IFilterChain interface.
    /// 
    /// A filter chain is a list of IFileFilter objects and it derives from the IFileFilter interface itself.
    /// It passes the input list of file through consecutively through each filter in the filter list.
    /// </summary>
    public interface IFilterChain : IFileFilter
    {
        /// <summary>
        /// List of file filters through which an enumeration of files is passed.
        /// </summary>
        IList<IFileFilter> Filters { get; }
    }

    /// <summary>
    /// IToken interface.
    /// 
    /// A token is a textual element. It has a type, it's text, a position in the text it originates from, a length 
    /// ( it may be longer than the actual token text ) and optionally a line and a column in which it occurs in the original text.
    /// 
    /// Multiple tokens are returned by a tokenizer.
    /// </summary>
    public interface IToken
    {
        /// <summary>
        /// Type of the token.
        /// </summary>
        string Type { get; }
        /// <summary>
        /// Text of the token. The token text may differ from the original text, however in this case the Position and Length
        /// properties should point to the actual text area in the original text
        /// </summary>
        string Text { get; }
        /// <summary>
        /// Position of the token in the original text.
        /// </summary>
        int Position { get; }
        /// <summary>
        /// Real length of the text element the token originated from.
        /// </summary>
        int Length { get; }
        /// <summary>
        /// Line number of the token. ( 0-based )
        /// </summary>
        int Line { get; }
        /// <summary>
        /// Column of the token within the line. ( 0-based )
        /// </summary>
        int Column { get; }
        /// <summary>
        /// Additional data associated with the token.
        /// </summary>
        object Data { get;  }
    }

    /// <summary>
    /// ITokenizer interface.
    /// 
    /// A tokenizer is responsible for extracting tokens from a text source depending on its configuration.
    /// 
    /// Typically an index is created from the textual data a tokenizer returns for a given text file.
    /// </summary>
    public interface ITokenizer
    {
        /// <summary>
        /// Enumerates all tokens contained in the text.
        /// </summary>
        /// <param name="inputStream">Input stream containing the textual data, from which tokens should be extracted.</param>
        /// <returns>An enumeration of tokens contained in the input stream.</returns>
        IEnumerable< IToken > EnumerateTokens( Stream inputStream );
        /// <summary>
        /// Enumerates all tokens contained in the text.
        /// </summary>
        /// <param name="inputReader">Input text reader, from which tokens should be extracted.</param>
        /// <returns>An enumeration of tokens contained in the input stream.</returns>
        IEnumerable<IToken> EnumerateTokens( TextReader inputReader );
    }

    /// <summary>
    /// IHighlighterToken interface.
    /// 
    /// A highlighter token is a token returned by a highlighter.
    /// </summary>
    public interface IHighlighterToken
    {
        /// <summary>
        /// Foreground color ( uint32: ARGB ), which should be used for highlighting the specified area of the text.
        /// </summary>
        uint Color { get; }
        /// <summary>
        /// Type of the token.
        /// </summary>
        string Type { get; }
        /// <summary>
        /// Start position of the highlighter token in the original text.
        /// </summary>
        int StartPosition { get; }
        /// <summary>
        /// End position of the highlighter token in the original text.
        /// </summary>
        int EndPosition { get; }
    }

    public class HighlighterToken : IHighlighterToken
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public HighlighterToken()
        {
        }


        /// <summary>
        /// Initialization constructor.
        /// </summary>
        public HighlighterToken( uint color, string type, int startPosition, int endPosition )
        {
            Color = color;
            Type = type;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        public HighlighterToken( uint color, int startPosition, int endPosition )
        {
            Color = color;
            Type = string.Empty;
            StartPosition = startPosition;
            EndPosition = endPosition;
        }

        /// <summary>
        /// Foreground color ( uint32: ARGB ), which should be used for highlighting the specified area of the text.
        /// </summary>
        public uint Color { get; set; }
        /// <summary>
        /// Type of the token.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Start position of the highlighter token in the original text.
        /// </summary>
        public int StartPosition { get; set; }
        /// <summary>
        /// End position of the highlighter token in the original text.
        /// </summary>
        public int EndPosition { get; set; }
    }

    /// <summary>
    /// IHighlighter interface.
    /// 
    /// A highlighter is responsible for returing highlight tokens for a given input text.
    /// 
    /// This interface may be used to provide syntax highlighting.
    /// </summary>
    public interface IHighlighter
    {
        /// <summary>
        /// Returns highlighter tokens for the specified text.
        /// </summary>
        /// <param name="text">Text, for which higlighter tokens should be returned.</param>
        /// <param name="activeBlock">Active multi line block.</param>
        /// <returns>Enumeration of highlighter tokens.</returns>
        IEnumerable<IHighlighterToken> Highlight( string text, IHighlighterToken activeBlock = null );
        /// <summary>
        /// Returns highlighter block tokens for the specified text.
        /// 
        /// This method should return all blocks spanning multiple lines of text.
        /// </summary>
        /// <param name="text">Text, for which highlighter block tokens should be returned.</param>
        /// <returns>Enumeration of highlighter block tokens.</returns>
        IEnumerable<IHighlighterToken> HighlightMultiLineBlocks( string text );
        /// <summary>
        /// Returns highlighter tokens for the text, which can be accessed by the text reader.
        /// </summary>
        /// <param name="inputReader">Text reader providing access to the text for which highlighter tokens should be generated.</param>
        /// <param name="activeBlock">Active multi line block.</param>
        /// <returns>Enumeration of highlighter tokens.</returns>
        IEnumerable<IHighlighterToken> Highlight( TextReader inputReader, IHighlighterToken activeBlock = null );
        /// <summary>
        /// Returns highlighter block tokens for the specified text.
        /// 
        /// This method should return all blocks spanning multiple lines of text.
        /// </summary>
        /// <param name="inputReader">Text reader providing access to the text for which highlighter block tokens should be generated.</param>
        /// <returns>Enumeration of highlighter block tokens.</returns>
        IEnumerable<IHighlighterToken> HighlightMultiLineBlocks( TextReader inputReader );
    }

    /// <summary>
    /// IIndexBuilder interface.
    /// 
    /// An index builder is responsible for creating a full text search index for a set of files.
    /// </summary>
    public interface IIndexBuilder : IDisposable
    {
        /// <summary>
        /// File path to the root of the index.
        /// </summary>
        string IndexPath { get;  }
        /// <summary>
        /// Adds a file to the index.
        /// </summary>
        /// <param name="filePath">Path of the file, which should be added to the index.</param>
        /// <param name="tokenizer">Tokenizer, which should be used to create text tokens from the file, which are then indexed.</param>
        void AddFile( string filePath, ITokenizer tokenizer );
        /// <summary>
        /// Updates an exisiting file in the index.
        /// </summary>
        /// <param name="filePath">Path of the file, which should be updated in the index.</param>
        /// <param name="tokenizer">Tokenizer, which should be used to create text tokens from the file, which are then indexed.</param>
        void UpdateFile( string filePath, ITokenizer tokenizer );
        /// <summary>
        /// Removes a file from the index.
        /// </summary>
        /// <param name="filePath">Path of the file, which should be removed from the index.</param>
        void RemoveFile( string filePath );
        /// <summary>
        /// Removes all files from the index.
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// Provides a symbol or an icon.
    /// </summary>
    public interface IIconProvider
    {
        /// <summary>
        /// Icon
        /// 
        /// Will be mostly used as a button symbol
        /// </summary>
        object Icon { get; }
    }

    /// <summary>
    /// Provides a caption or display label
    /// </summary>
    public interface ICaptionProvider
    {
        /// <summary>
        /// Caption.
        /// 
        /// Will be mostly used a a display label
        /// </summary>
        string Caption { get; }
    }

    /// <summary>
    /// Provides a description or a tool tip
    /// </summary>
    public interface IDescriptionProvider
    {
        /// <summary>
        /// Description.
        /// 
        /// Will be mostly used as a tooltip text
        /// </summary>
        string Description { get; }
    }

    /// <summary>
    /// Option interface
    /// </summary>
    public interface IOption
    {
        /// <summary>
        /// Option name
        /// </summary>
        string Name { get; }
        /// <summary>
        /// Type of the option value
        /// </summary>
        Type   ValueType { get; }
    }

    /// <summary>
    /// Typed Option interface
    /// </summary>
    /// <typeparam name="VALUE_TYPE">Option value type.</typeparam>
    public interface IOption<VALUE_TYPE> : IOption
    {
    }

    /// <summary>
    /// Option implementation.
    /// </summary>
    /// <typeparam name="VALUE_TYPE">Option value type.</typeparam>
    public class Option<VALUE_TYPE> : IOption<VALUE_TYPE>, ICaptionProvider, IDescriptionProvider, IIconProvider
    {
        public Option( string name, string caption = null, string description = null, object icon = null ) 
        { 
            Name = name;
            Caption = caption ?? name;
            Description = description ?? ( caption ?? name );
            Icon = icon;
        }
           
        public string Name { get; private set;  }
        public string Caption { get; private set; }
        public string Description { get; private set; }
        public object Icon { get; private set; }
        public Type ValueType { get { return typeof( VALUE_TYPE ); } }
    }

    /// <summary>
    /// Standard search options.
    /// </summary>
    public static class SearchOptions
    {
        /// <summary>
        /// Case sensitive search.
        /// </summary>
        public readonly static IOption CaseSensitive = new Option<bool>( "CaseSensitive", "Case sensitive", "Determines, whether search should be case sensitive or not.", "/CodeXCavator.UI;component/Images/text_lowercase.png" );
        /// <summary>
        /// Word wise search.
        /// </summary>
        public readonly static IOption WordWise = new Option<bool>( "WordWise", "Whole word", "Determines, whether whole words should be searched, or not.", "/CodeXCavator.UI;component/Images/spellcheck.png" );
    }

    public delegate void OptionChangedEvent( IOptionsProvider optionsProvider, IOption option );
    /// <summary>
    /// Options provider.
    /// </summary>
    public interface IOptionsProvider
    {
        /// <summary>
        /// Returns the set of provided options.
        /// </summary>
        IEnumerable<IOption> Options { get; }
        /// <summary>
        /// Sets an option value.
        /// </summary>
        /// <param name="option">Option, which should be set.</param>
        /// <param name="value">Value of the option, which should be set.</param>
        void SetOptionValue( IOption option, object value );
        /// <summary>
        /// Returns an option value.
        /// </summary>
        /// <param name="option">Option, whose value should be returned.</param>
        /// <returns>Value of the option.</returns>
        object GetOptionValue( IOption option );

        /// <summary>
        /// Notifies of option changes.
        /// </summary>
        event OptionChangedEvent OptionChanged;
    }

    /// <summary>
    /// Extension functions for IOptions interface.
    /// </summary>
    public static class IOptionsProviderExtensions
    {
        /// <summary>
        /// Sets an option value by name.
        /// </summary>
        /// <param name="optionsProvider">Options provider, whose option value should be set.</param>
        /// <param name="optionName">Name of the option, whose value should be set.</param>
        /// <param name="value">Value of the option, which should be set</param>
        public static void SetOptionValue( this IOptionsProvider optionsProvider, string optionName, object value )
        {
            if( optionsProvider == null )
                return;
            var option = optionsProvider.Options.FirstOrDefault( searchedOption => searchedOption.Name.Equals( optionName ) );
            if( option != null )
                optionsProvider.SetOptionValue( option, value );
        }

        /// <summary>
        /// Returns an option value by name.
        /// </summary>
        /// <param name="optionsProvider">Options provider, from which an option value should be retrieved.</param>
        /// <param name="optionName">Name of the option, whose value should be retrieved.</param>
        /// <returns>Option value, or null, if the option does not exist.</returns>
        public static object GetOptionValue( this IOptionsProvider optionsProvider, string optionName )
        {
            if( optionsProvider == null )
                return null;
            var option = optionsProvider.Options.FirstOrDefault( searchedOption => searchedOption.Name.Equals( optionName ) );
            if( option != null )
               return optionsProvider.GetOptionValue( option );
            return null;
        }

        /// <summary>
        /// Returns an option value typed.
        /// </summary>
        /// <param name="optionsProvider">Options provider, from which an option value should be retrieved.</param>
        /// <param name="option">Option, whose value should be retrieved.</param>
        /// <returns>Option value, or default(RETURN_TYPE), if the option does not exist.</returns>
        public static RETURN_TYPE GetOptionValue<RETURN_TYPE>( this IOptionsProvider optionsProvider, IOption option )
        {
            if( optionsProvider == null )
                return default(RETURN_TYPE);
            if( option != null )
                return (RETURN_TYPE) optionsProvider.GetOptionValue( option );
            return default(RETURN_TYPE);
        }

        /// <summary>
        /// Returns an option value typed by name.
        /// </summary>
        /// <param name="optionsProvider">Options provider, from which an option value should be retrieved.</param>
        /// <param name="optionName">Name of the option, whose value should be retrieved.</param>
        /// <returns>Option value, or default(RETURN_TYPE), if the option does not exist.</returns>
        public static RETURN_TYPE GetOptionValue<RETURN_TYPE>( this IOptionsProvider optionsProvider, string optionName )
        {
            var optionValue = GetOptionValue( optionsProvider, optionName );
            if( optionValue != null )
                return (RETURN_TYPE) optionValue;
            return default(RETURN_TYPE);
        }

    }

    /// <summary>
    /// IOccurrence interface.
    /// 
    /// An occurrence is a search result occurrence in a single file.
    /// </summary>
    public interface IOccurrence
    {
        /// <summary>
        /// Line number of the occurrence. ( 0-based ).
        /// </summary>
        int Line { get;  }
        /// <summary>
        /// Column number of the occurrence ( 0-based ).
        /// </summary>
        int Column { get; }
        /// <summary>
        /// Occurrence text matching the search result.
        /// </summary>
        string Match { get;  }
        /// <summary>
        /// Fragment of consisting of multiple lines containing the occurrence.
        /// </summary>
        KeyValuePair<string, int>[] Fragment { get; }
    }

    /// <summary>
    /// ISearchHit interface.
    /// 
    /// A search hit is a search query hit. It contains the file, which satisified the search query and
    /// all occurrences matching the search query within the file.
    /// </summary>
    public interface ISearchHit
    {
        /// <summary>
        /// File path of the file, which satisified a search query.
        /// </summary>
        string FilePath { get; }
        /// <summary>
        /// Score of the file. The higher the score, the more the file has satisified the search query.
        /// </summary>
        double Score { get;  }
        /// <summary>
        /// List of all occurrences in the file, which matched the search query.
        /// </summary>
        ReadOnlyCollection<IOccurrence> Occurrences { get; }
    }

    /// <summary>
    /// Searchg types
    /// </summary>
    public static class SearchType
    {
        /// <summary>
        /// Search files.
        /// </summary>
        public const string Files = "Path";
        /// <summary>
        /// Search file contents.
        /// </summary>
        public const string Contents = "Contents";
        /// <summary>
        /// Search file tags.
        /// </summary>
        public const string Tags = "Tags";
    }

    /// <summary>
    /// Searcher interface.
    /// 
    /// A searcher is responsible for searching text in a text stream or string.
    /// </summary>
    public interface ITextSearcher : IOptionsProvider
    {
        /// <summary>
        /// Determines, whether the specified search query is valid.
        /// </summary>
        /// <param name="searchQuery">Search query, which should be validated.</param>
        /// <returns>True, if the search query is valid, false otherwise.</returns>
        bool IsValidSearchQuery( string searchQuery );

        /// <summary>
        /// Searches the text stream for the occurrences matching the search query.
        /// </summary>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>Enumeration of files, which matched the search query.</returns>
        IEnumerable<IOccurrence> Search( TextReader textReader, string searchQuery );

        /// <summary>
        /// Searches the text for the occurrences matching the search query.
        /// </summary>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>Enumeration of files, which matched the search query.</returns>
        IEnumerable<IOccurrence> Search( string text, string searchQuery );
    }


    /// <summary>
    /// IIndexSearcher interface.
    /// 
    /// An index searcher is responsible for searching an indexed previously created with an index builder for a matches
    /// of a given search query.
    /// </summary>
    public interface IIndexSearcher : IOptionsProvider, IDisposable
    {
        /// <summary>
        /// Determines, whether the specified search query is valid.
        /// </summary>
        /// <param name="searchQuery">Search query, which should be validated.</param>
        /// <returns>True, if the search query is valid, false otherwise.</returns>
        bool IsValidSearchQuery( string searchQuery );

        /// <summary>
        /// Determines, whether the specified search quey is a valid file search query.
        /// </summary>
        /// <param name="searchQuery">Search query, which should be validated.</param>
        /// <returns>True, if the search query is valid, false otherwise.</returns>
        bool IsValidFileSearchQuery( string searchQuery );

        /// <summary>
        /// Searches the index for matches of the specified search query. 
        /// </summary>
        /// <param name="searchType">Type of the search. See also SearchType class.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>Enumeration of files, which matched the search query.</returns>
        IEnumerable<ISearchHit> Search( string searchType, string searchQuery );
        /// <summary>
        /// Searches the index for matches of the specified search query. 
        /// </summary>
        /// <param name="searchType">Type of the search. See also SearchType class.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <param name="numHits">Reference to a variable, which should store the total number of files, which matched the search query.</param>
        /// <returns>Enumeration of files, which matched the search query.</returns>
        IEnumerable<ISearchHit> Search( string searchType, string searchQuery, out int numHits );
        /// <summary>
        /// Searches the index for matches of the specified search query. 
        /// </summary>
        /// <param name="searchType">Type of the search. See also SearchType class.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <param name="numHits">Reference to a variable, which should store the total number of files, which matched the search query.</param>
        /// <param name="allowedFileTypes">List of file extensions. This list limits the search only to the specified file types.</param>
        /// <returns>Enumeration of files, which matched the search query.</returns>
        IEnumerable<ISearchHit> Search( string searchType, string searchQuery, out int numHits, params string[] allowedFileTypes );
        /// <summary>
        /// Searches the index for matches of the specified search query. 
        /// </summary>
        /// <param name="searchType">Type of the search. See also SearchType class.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <param name="numHits">Reference to a variable, which should store the total number of files, which matched the search query.</param>
        /// <param name="allowedFileTypes">List of file extensions. This list limits the search only to the specified file types.</param>
        /// <param name="directories">List of directories. The tuple contains the directory wildcard pattern, a recursive flag, and an exclude flag. The search is only limited to the specified directories.</param>
        /// <returns>Enumeration of files, which matched the search query.</returns>
        IEnumerable<ISearchHit> Search( string searchType, string searchQuery, out int numHits, string[] allowedFileTypes, Tuple<string, bool, bool>[] directories );
    }

    /// <summary>
    /// Link info structure.
    /// </summary>
    public class LinkInfo : ICaptionProvider
    {
        // Link url
        public string Url { get; set; }
        // Link caption
        public string Caption { get; set; }


        public override bool Equals( object obj )
        {
            var otherLinkInfo = obj as LinkInfo;
            if( otherLinkInfo == null )
                return false;
            return object.Equals( Url, otherLinkInfo.Url ) && object.Equals( Caption, otherLinkInfo.Caption );
        }

        public override int GetHashCode()
        {
            var urlHashCode = Url != null ? Url.GetHashCode() : 0;
            var captionHashCode = Caption != null ? Caption.GetHashCode() : 0;
            unchecked { return ( urlHashCode * 397 ) ^ captionHashCode; }
        }
    }

    /// <summary>
    /// Tag info structure
    /// </summary>
    public class TagInfo
    {
        // Tag
        public string Tag { get; set; }
        // Total number of tag occurrences
        public int TotalCount { get; set; }
        // Number of documents containing the tag.
        public int DocumentCount { get; set; }
        /// <summary>
        /// Links associated with the tag.
        /// </summary>
        public IEnumerable<LinkInfo> Links { get; set; }
    }

    /// <summary>
    /// IIndex interface.
    /// 
    /// An index consists of full text search data for multiple files.
    /// </summary>
    public interface IIndex : IDisposable
    {
        /// <summary>
        /// File path to the root of the index.
        /// </summary>
        string IndexPath { get; }
        /// <summary>
        /// List of files contained in the index.
        /// </summary>
        IEnumerable<string> Files { get; }
        /// <summary>
        /// List of tags contained in the index.
        /// </summary>
        IEnumerable<TagInfo> Tags  { get; }
        /// <summary>
        /// List of file type contained in the index.
        /// </summary>
        IEnumerable<string> FileTypes { get; }
        /// <summary>
        /// Creates an index searcher for the index.
        /// </summary>
        /// <returns>IIndexSearcher instance, which can be used to search the index.</returns>
        IIndexSearcher CreateSearcher();
        /// <summary>
        /// Creates an index searcher for the index.
        /// </summary>
        /// <param name="searchOptions">Search options from which the searcher should be initialized.</param>
        /// <returns>IIndexSearcher instance, which can be used to search the index.</returns>
        IIndexSearcher CreateSearcher( IOptionsProvider searchOptions );
        /// <summary>
        /// Creates an index builder for the index.
        /// </summary>
        /// <param name="overwrite">Specifies, whether the current contents of the index, should be discarded.</param>
        /// <returns>IIndexBuilder instance, which can be used to update or construct the index.</returns>
        IIndexBuilder CreateBuilder( bool overwrite = false );
        /// <summary>
        /// Returns tag information for the specified tag.
        /// </summary>
        /// <param name="tag">Tag for which information should be returned.</param>
        /// <returns>TagInfo structure, or null if tag does not exists.</returns>
        TagInfo GetTagInfo( string tag );
    }

    /// <summary>
    /// IProgressProvider interface.
    /// 
    /// This interface, can be implemented if progress information sould be returned.
    /// </summary>
    public interface IProgressProvider
    {
        /// <summary>
        /// OnProgress event.
        /// 
        /// This event should be triggered every time progress information sould be returned.
        /// </summary>
        event Action< object, string > OnProgress;
    }

    /// <summary>
    /// IConfigurable interface.
    /// 
    /// A configurable object, can be configured from a certain type of input data.
    /// </summary>
    /// <typeparam name="CONFIGURATION_TYPE">Type of data, from which the configurable can be configured.</typeparam>
    public interface IConfigurable<CONFIGURATION_TYPE> where CONFIGURATION_TYPE: class, new()
    {
        /// <summary>
        /// Configures the configurable object.
        /// </summary>
        /// <param name="configuration">Configuration from which the configurable object should be configured.</param>
        void Configure( CONFIGURATION_TYPE configuration );
    }

    /// <summary>
    /// IConfigurable extensions
    /// </summary>
    public static class IConfigurableExtensions
    {
        /// <summary>
        /// Checks, whether the specified object is an IConfigurable.
        /// </summary>
        /// <param name="configurable">Configurable object.</param>
        /// <returns>True, if the object is configurable, false otherwise.</returns>
        public static bool IsConfigurable( this object configurable )
        {
            if( configurable == null )
                return false;

            foreach( var implementedInterface in configurable.GetType().GetInterfaces() )
            {
                if( implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeof( IConfigurable<> ) )
                    return true;
            }   

            return false;
        }

        /// <summary>
        /// Returns the configuration object type for the specified configurable.
        /// </summary>
        /// <param name="configurable">Configurable, for which the configuration object type, should be retrieved.</param>
        /// <returns>Configuration object type for the configurable object, or null, if the object is not configurable.</returns>
        public static Type GetConfigurationType( this Type configurableType )
        {
            if( configurableType == null )
                return null;

            foreach( var implementedInterface in configurableType.GetInterfaces() )
            {
                if( implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeof( IConfigurable<> ) )
                    return implementedInterface.GetGenericArguments()[0];
            }   
            
            return null;
        }

        /// <summary>
        /// Creates and configures a configurable object.
        /// </summary>
        /// <param name="configurableType">Type of the configurable object, which should be created.</param>
        /// <param name="configuration">Configuration object.</param>
        /// <returns>Created and configured object, or null, if the object cannot be created or configured.</returns>
        public static object CreateAndConfigure( this Type configurableType, object configuration )
        {
            if( configurableType == null )
                return null;

            try
            {
                var configurableObject = Activator.CreateInstance( configurableType );
                if( configurableObject != null )
                {
                    var configurationType = configuration != null ? configuration.GetType() : null;
                    var expectedConfigurationType = GetConfigurationType( configurableType );
                    if( expectedConfigurationType != null && expectedConfigurationType.IsAssignableFrom( configurationType ) )
                    {
                        var configureMethod = configurableType.GetMethod( "Configure", BindingFlags.Public | BindingFlags.Instance, null, new Type[] { expectedConfigurationType }, null );
                        configureMethod.Invoke( configurableObject, new object[] { configuration } );
                    }                                
                }
                return configurableObject;
            }
            catch
            {
            }                
            return null;
        }

        /// <summary>
        /// Creates the configurable type, and configures it by reading information from a configuration xml element.
        /// </summary>
        /// <param name="configurableType">Configurable type, which should be created and configured.</param>
        /// <param name="configurationElement">Xml element containing configuration information.</param>
        /// <returns>Configured instance of the specified type, or null, if the instance cannot be created or configured.</returns>
        public static object CreateAndConfigure( this Type configurableType, XElement configurationElement )
        {
            return CreateAndConfigure( configurableType, LoadConfiguration( GetConfigurationType( configurableType ), configurationElement ) );
        }

        private const string XML_ELEMENT_CONFIGURATION = "Configuration";
    
        /// <summary>
        /// Loads a configuration.
        /// </summary>
        /// <param name="configurationType">Highlighter configuration type.</param>
        /// <param name="configurationElement">Configuration element.</param>
        /// <returns>Configuration object loaded from the configuration element.</returns>
        private static object LoadConfiguration( Type configurationType, XElement configurationElement )
        {
            try
            {
                if( configurationType != null && configurationElement != null )
                {
                    XmlSerializer configurationSerializer = new XmlSerializer( configurationType, new XmlRootAttribute( XML_ELEMENT_CONFIGURATION ) );
                    var configuration = configurationSerializer.Deserialize( configurationElement.CreateReader() );
                    return configuration;
                }
            }
            catch
            {
            }
            return null;
        }

    }

    /// <summary>
    /// Extension functions for IIndexBuilder interface.
    /// </summary>
    public static class IIndexBuilderExtensions
    {

        internal const string XML_ELEMENT_INDEX = "Index";
        internal const string XML_ATTIRBUTE_PATH = "Path";
        internal const string XML_ATTRIBUTE_FILE_SOURCES = CompoundFileEnumeratorBuilder.XML_ELEMENT_FILE_SOURCES;

        /// <summary>
        /// Creates an index builder form an XML configuration file.
        /// </summary>
        /// <param name="fileName">Name of the XML configuration file, from which an index builder should be created.</param>
        /// <param name="inputFiles">Reference to a variable, which should recieve an enumeration of the input files, which should be added to the index.</param>
        /// <returns>IIndexBuilder instance, which can be fed with the returned input files.</returns>
        public static IIndexBuilder CreateFromXmlFile( string fileName, out IEnumerable<string> inputFiles )
        {
            using( var stream = new System.IO.FileStream( fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read ) )
            {
                var indexBuilder = CreateFromXml( stream, out inputFiles );
                stream.Close();
                return indexBuilder;
            }
        }

        /// <summary>
        /// Creates an index builder from an XML configuration.
        /// </summary>
        /// <param name="stream">Stream containing XML configuration, from which an index builder should be created.</param>
        /// <param name="inputFiles">Reference to a variable, which should recieve an enumeration of the input files, which should be added to the index.</param>
        /// <returns>IIndexBuilder instance, which can be fed with the returned input files.</returns>
        public static IIndexBuilder CreateFromXml( System.IO.Stream stream, out IEnumerable<string> inputFiles )
        {
            return CreateFromXml( XDocument.Load( stream ).Root, out inputFiles );
        }

        /// <summary>
        /// Creates an index builder from an XML configuration.
        /// </summary>
        /// <param name="reader">Reader providing access to an XML configuration, from which an index builder should be created.</param>
        /// <param name="inputFiles">Reference to a variable, which should recieve an enumeration of the input files, which should be added to the index.</param>
        /// <returns>IIndexBuilder instance, which can be fed with the returned input files.</returns>
        public static IIndexBuilder CreateFromXml( System.IO.TextReader reader, out IEnumerable<string> inputFiles )
        {
            return CreateFromXml( XDocument.Load( reader ).Root, out inputFiles );
        }

        /// <summary>
        /// Creates an index builder from an XML configuration.
        /// </summary>
        /// <param name="xml">XML string containing XML configuration, from which an index builder should be created.</param>
        /// <param name="inputFiles">Reference to a variable, which should recieve an enumeration of the input files, which should be added to the index.</param>
        /// <returns>IIndexBuilder instance, which can be fed with the returned input files.</returns>
        public static IIndexBuilder CreateFromXml( string xml, out IEnumerable<string> inputFiles )
        {
            return CreateFromXml( XDocument.Parse( xml ).Root, out inputFiles );
        }

        /// <summary>
        /// Creates an index builder from an XML configuration.
        /// </summary>
        /// <param name="root">Root element of the XML configuration, from which an index builder should be created.</param>
        /// <param name="inputFiles">Reference to a variable, which should recieve an enumeration of the input files, which should be added to the index.</param>
        /// <returns>IIndexBuilder instance, which can be fed with the returned input files.</returns>
        public static IIndexBuilder CreateFromXml( XElement root, out IEnumerable<string> inputFiles )
        {
            inputFiles = null;

            // Check root element of the configuration
            if( root != null && root.Name.LocalName == XML_ELEMENT_INDEX )
            {
                // Get index path
                XAttribute pathAttribute = root.Attribute( XML_ATTIRBUTE_PATH );
                if( pathAttribute != null && pathAttribute.Value != null )
                {
                    string path = pathAttribute.Value.Trim();
                    if( !string.IsNullOrEmpty( path ) )
                    {
                        // Create a file system based index builder
                        var indexBuilder = IndexFactory.CreateFileSystemIndexBuilder( Environment.ExpandEnvironmentVariables(path), true );
                        // Create compound file enumerator from file source elements
                        var fileEnumerator = CompoundFileEnumeratorBuilder.BuildFromXml( root.Elements().Where( element => element.Name.LocalName == XML_ATTRIBUTE_FILE_SOURCES ).FirstOrDefault() );
                        if( fileEnumerator != null )
                            inputFiles = fileEnumerator.EnumerateFiles();
                        return indexBuilder;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a file to the index.
        /// 
        /// This method determines a tokenizer for the specified file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index to which a file should be added.</param>
        /// <param name="filePath">Path to the file, which should be added.</param>
        public static void AddFile( this IIndexBuilder index, string filePath )
        {
            var tokenizer = FileTokenizers.GetFileTokenizerForPath( filePath );
            index.AddFile( filePath, tokenizer );
        }

        /// <summary>
        /// Updates a file in the index.
        /// 
        /// This method determines a tokenizer for the specified file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index to which a file should be added.</param>
        /// <param name="filePath">Path to the file, which should be added.</param>
        public static void UpdateFile( this IIndexBuilder index, string filePath )
        {
            var tokenizer = FileTokenizers.GetFileTokenizerForPath( filePath );
            index.UpdateFile( filePath, tokenizer );
        }

        /// <summary>
        /// Adds multiple files to the index.
        /// 
        /// This method determines a tokenizer for each specified file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index to which files should be added.</param>
        /// <param name="files">List of files, which should be added to the index.</param>
        public static void AddFiles( this IIndexBuilder index, IEnumerable<string> files )
        {
            foreach( var filePath in files )
                index.AddFile( filePath );
        }

        /// <summary>
        /// Adds multiple files from a file catalogue to the index.
        /// 
        /// This method determines a file catalogue enumerator for the specified file catalogue, by querying
        /// the file catalogue enumerator registry.
        /// 
        /// This method also determines a tokenizer for each file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index to which files should be added.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be added to the index.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void AddFiles( this IIndexBuilder index, string fileCataloguePath, bool recursive )
        {
            var enumerator = FileCatalogueEnumerators.GetFileCatalogueEnumeratorForPath( fileCataloguePath );
            if( enumerator != null )
            {
                foreach( var filePath in enumerator.EnumerateFiles( fileCataloguePath, recursive ) )
                    index.AddFile( filePath );
            }
        }

        /// <summary>
        /// Adds multiple files from a file catalogue to the index.
        /// 
        /// This method determines a tokenizer for each file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index to which files should be added.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be added to the index.</param>
        /// <param name="fileEnumerator">File catalogue enumerator, which should be used to enumerate the files contained in the specified catalogue.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void AddFiles( this IIndexBuilder index, string fileCataloguePath, IFileCatalogueEnumerator fileEnumerator, bool recursive )
        {
            if( fileEnumerator != null )
                AddFiles( index, fileEnumerator.EnumerateFiles( fileCataloguePath, recursive ) );
        }

        /// <summary>
        /// Adds multiple files from a file catalogue to the index.
        /// 
        /// This method determines a tokenizer for each file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <typeparam name="FILECATALOGUE_ENUMERATOR_TYPE">Type of the file catalgoue enumerator, to be used for enumerating the file catalogue.</typeparam>
        /// <param name="index">Index to which files should be added.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be added to the index.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void AddFiles<FILECATALOGUE_ENUMERATOR_TYPE>( this IIndexBuilder index, string fileCataloguePath, bool recursive ) where FILECATALOGUE_ENUMERATOR_TYPE : IFileCatalogueEnumerator, new()
        {
            AddFiles( index, fileCataloguePath, new FILECATALOGUE_ENUMERATOR_TYPE(), recursive );
        }

        /// <summary>
        /// Updates multiple files in the index.
        /// 
        /// This method determines a tokenizer for each file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index, which should be updated.</param>
        /// <param name="files">List of files, which should be updated.</param>
        public static void UpdateFiles( this IIndexBuilder index, IEnumerable< string > files )
        {
            foreach( var filePath in files )
                index.UpdateFile( filePath );
        }

        /// <summary>
        /// Updates multiple files in the index.
        /// 
        /// This method determines a file catalogue enumerator for the specified file catalogue, by querying
        /// the file catalogue enumerator registry.
        /// 
        /// This method also determines a tokenizer for each file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index, which should be updated.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be updated in the index.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void UpdateFiles( this IIndexBuilder index, string fileCataloguePath, bool recursive )
        {
            var enumerator = FileCatalogueEnumerators.GetFileCatalogueEnumeratorForPath( fileCataloguePath );
            if( enumerator != null )
            {
                foreach( var filePath in enumerator.EnumerateFiles( fileCataloguePath, recursive ) )
                    index.UpdateFile( filePath );
            }
        }

        /// <summary>
        /// Updates multiple files in the index.
        /// 
        /// This method determines a tokenizer for each file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <param name="index">Index, which should be updated.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be updated in the index.</param>
        /// <param name="fileEnumerator">File catalogue enumerator, which should be used to enumerate the files contained in the specified catalogue.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void UpdateFiles( this IIndexBuilder index, string fileCataloguePath, IFileCatalogueEnumerator fileEnumerator, bool recursive )
        {
            if( fileEnumerator != null )
                UpdateFiles( index, fileEnumerator.EnumerateFiles( fileCataloguePath, recursive ) );
        }

        /// <summary>
        /// Updates multiple files in the index.
        /// 
        /// This method determines a tokenizer for each file, by querying
        /// the tokenizer registry.
        /// </summary>
        /// <typeparam name="FILECATALOGUE_ENUMERATOR_TYPE">Type of the file catalgoue enumerator, to be used for enumerating the file catalogue.</typeparam>
        /// <param name="index">Index, which should be updated.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be updated in the index.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void UpdateFiles<FILECATALOGUE_ENUMERATOR_TYPE>( this IIndexBuilder index, string fileCataloguePath, bool recursive ) where FILECATALOGUE_ENUMERATOR_TYPE : IFileCatalogueEnumerator, new()
        {
            UpdateFiles( index, fileCataloguePath, new FILECATALOGUE_ENUMERATOR_TYPE(), recursive );
        }
        
        /// <summary>
        /// Removes multiple files from the index.
        /// </summary>
        /// <param name="index">Index, from which files should be removed.</param>
        /// <param name="files">List of files, which should be removed from the index.</param>
        public static void RemoveFiles( this IIndexBuilder index, IEnumerable< string > files )
        {
            foreach( var filePath in files )
                index.RemoveFile( filePath );
        }

        /// <summary>
        /// Removes multiple files from the index.
        /// 
        /// This method determines a file catalogue enumerator for the specified file catalogue, by querying
        /// the file catalogue enumerator registry.
        /// </summary>
        /// <param name="index">Index, from which files should be removed.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be removed from the index.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void RemoveFiles( this IIndexBuilder index, string fileCataloguePath, bool recursive )
        {
            var enumerator = FileCatalogueEnumerators.GetFileCatalogueEnumeratorForPath( fileCataloguePath );
            if( enumerator != null )
            {
                foreach( var filePath in enumerator.EnumerateFiles( fileCataloguePath, recursive ) )
                    index.RemoveFile( filePath );
            }
        }

        /// <summary>
        /// Removes multiple files from the index.
        /// </summary>
        /// <param name="index">Index, from which files should be removed.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be removed from the index.</param>
        /// <param name="fileEnumerator">File catalogue enumerator, which should be used to enumerate the files contained in the specified catalogue.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void RemoveFiles( this IIndexBuilder index, string fileCataloguePath, IFileCatalogueEnumerator fileEnumerator, bool recursive )
        {
            if( fileEnumerator != null )
                RemoveFiles( index, fileEnumerator.EnumerateFiles( fileCataloguePath, recursive ) );
        }

        /// <summary>
        /// Removes multiple files from the index.
        /// </summary>
        /// <typeparam name="FILECATALOGUE_ENUMERATOR_TYPE">Type of the file catalgoue enumerator, to be used for enumerating the file catalogue.</typeparam>
        /// <param name="index">Index, from which files should be removed.</param>
        /// <param name="fileCataloguePath">Path to the file catalogue, which contains the file names of the files, which should be removed from the index.</param>
        /// <param name="recursive">Indicates, whether the file catalogue enumerator should enumerate the catalogue recursively, or not.</param>
        public static void RemoveFiles<FILECATALGOUE_ENUMERATOR_TYPE>( this IIndexBuilder index, string fileCataloguePath, bool recursive ) where FILECATALGOUE_ENUMERATOR_TYPE : IFileCatalogueEnumerator, new()
        {
            RemoveFiles( index, fileCataloguePath, new FILECATALGOUE_ENUMERATOR_TYPE(), recursive );
        }
    }

    /// <summary>
    /// Extension functions for IIndex interface.
    /// </summary>
    public static class IIndexExtensions
    {

        internal const string XML_ELEMENT_INDEX = "Index";
        internal const string XML_ATTIRBUTE_PATH = "Path";
        internal const string XML_ATTRIBUTE_FILE_SOURCES = CompoundFileEnumeratorBuilder.XML_ELEMENT_FILE_SOURCES;

        /// <summary>
        /// Creates an index from an XML configuration file.
        /// </summary>
        /// <param name="fileName">File name of the XML configuration file, from which an IIndex instance should be created.</param>
        /// <returns>IIndex instance created from the specified XML configuration.</returns>
        public static IIndex CreateFromXmlFile( string fileName )
        {
            using( var stream = new System.IO.FileStream( fileName, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read ) )
            {
                var index = CreateFromXml( stream );
                stream.Close();
                return index;
            }
        }

        /// <summary>
        /// Creates an index from an XML configuration.
        /// </summary>
        /// <param name="fileName">Stream containing XML configuration, from which an IIndex instance should be created.</param>
        /// <returns>IIndex instance created from the specified XML configuration.</returns>
        public static IIndex CreateFromXml( System.IO.Stream stream )
        {
            return CreateFromXml( XDocument.Load( stream ).Root );
        }

        /// <summary>
        /// Creates an index from an XML configuration.
        /// </summary>
        /// <param name="reader">Text reader providing access to an XML configuration, from which an IIndex instance should be created.</param>
        /// <returns>IIndex instance created from the specified XML configuration.</returns>
        public static IIndex CreateFromXml( System.IO.TextReader reader )
        {
            return CreateFromXml( XDocument.Load( reader ).Root );
        }

        /// <summary>
        /// Creates an index from an XML configuration.
        /// </summary>
        /// <param name="xml">String containing an XML configuration, from which an IIndex instance should be created.</param>
        /// <returns>IIndex instance created from the specified XML configuration.</returns>
        public static IIndex CreateFromXml( string xml )
        {
            return CreateFromXml( XDocument.Parse( xml ).Root  );
        }

        /// <summary>
        /// Creates an index from an XML configuration.
        /// </summary>
        /// <param name="root">Root element of the XML configuration, from which an IIndex instance should be created.</param>
        /// <returns>IIndex instance created from the specified XML configuration.</returns>
        public static IIndex CreateFromXml( XElement root )
        {
            // Check root element.
            if( root != null && root.Name.LocalName == XML_ELEMENT_INDEX )
            {
                // Get index path from XML attribute
                XAttribute pathAttribute = root.Attribute( XML_ATTIRBUTE_PATH );
                if( pathAttribute != null && pathAttribute.Value != null )
                {
                    string path = pathAttribute.Value.Trim();
                    if( !string.IsNullOrEmpty( path ) )
                    {
                        // Create a file system based index.
                        var indexBuilder = IndexFactory.CreateFileSystemIndex( Environment.ExpandEnvironmentVariables( path ), false );
                        return indexBuilder;
                    }
                }
            }

            return null;
        }

    }

    /// <summary>
    /// IFileAction interface.
    /// </summary>
    public interface IFileAction : IDescriptionProvider, ICaptionProvider
    {
        /// <summary>
        /// Name of the action.
        /// </summary>
        string Name { get;  }

        /// <summary>
        /// Checks, whether the action can be executed for the specified file.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>True, if the action can be executed for the specified path. False otherwise.</returns>
        bool CanExecute( string filePath );
        /// <summary>
        /// Executes the action for the specified file.
        /// </summary>
        /// <param name="filePath"></param>
        void Execute( string filePath );
    }

    /// <summary>
    /// User settings provider
    /// </summary>
    public interface IUserSettingsStorageProvider
    {
        /// <summary>
        /// Restores user settings located below the specified path.
        /// </summary>
        /// <param name="path">Path, below which the user settings are located.</param>
        /// <param name="settings">User settings object, to which the user settings should be restored.</param>
        void Restore( string path, object settings );
        /// <summary>
        /// Stores user settings under the specified path.
        /// </summary>
        /// <param name="path">Path, to which the user settings should be written.</param>
        /// <param name="settings">User settings object, which should be stored.</param>
        void Store( string path, object settings );
    }
}
