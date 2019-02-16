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
using Lucene.Net.QueryParsers;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CodeXCavator.Engine.Searchers
{

    /// <summary>
    /// Query text searcher
    /// 
    /// The QueryTextSearcher searches for occurrences matching a Lucene search query.
    /// </summary>
    public class QueryTextSearcher : ICaptionProvider, IDescriptionProvider, IIconProvider, ITextSearcher
    {

        /// <summary>
        /// Caption of the text searcher.
        /// </summary>
        string ICaptionProvider.Caption
        {
            get { return "Search query"; }
        }

        /// <summary>
        /// Description of the text searcher.
        /// </summary>
        string IDescriptionProvider.Description
        {
            get { return "Searches for text occurrences matching a search query."; }
        }

        private static ImageSource mIcon = new BitmapImage( new Uri( "/CodeXCavator.Engine;component/Images/find_indexed.png", UriKind.RelativeOrAbsolute ) );

        /// <summary>
        /// Icon of the text searcher.
        /// </summary>
        object IIconProvider.Icon
        {
            get
            {
                return mIcon;
            }
        }

        /// <summary>
        /// Checks, whether the search query is valid.
        /// </summary>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>True, if valid, false otherwise.</returns>
        public bool IsValidSearchQuery( string searchQuery )
        {
            try
            {
                LuceneIndexSearcher.DEFAULT_CONTENT_PARSER.Parse( searchQuery );
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Searches the text provided by the text reader for matches of the search query.
        /// </summary>
        /// <param name="textReader">Text reader providing text to be searched.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>Enumeration of matches of the search query.</returns>
        public IEnumerable<IOccurrence> Search( System.IO.TextReader textReader, string searchQuery )
        {
            return Search( textReader.ReadToEnd(), searchQuery );
        }

        /// <summary>
        /// Searches the text for matches of the search query.
        /// </summary>
        /// <param name="textReader">Text to be searched.</param>
        /// <param name="searchQuery">Search query.</param>
        /// <returns>Enumeration of matches of the search query.</returns>
        public IEnumerable<IOccurrence> Search( string text, string searchQuery )
        {
            return LuceneSearchUtilities.DetermineOccurrences( SearchType.Contents, LuceneIndexSearcher.DEFAULT_CONTENT_PARSER.Parse( searchQuery ), text, LuceneIndex.DEFAULT_FILEPATH_ANALYZER, LuceneIndex.DEFAULT_CONTENTS_ANALYZER, LuceneIndex.DEFAULT_TAGS_ANALYZER, false );
        }

        /// <summary>
        /// List of supported search options.
        /// </summary>
        public static readonly IEnumerable<IOption> SupportedSearchOptions = new IOption[] { SearchOptions.CaseSensitive };

        /// <summary>
        /// List of supported search options.
        /// </summary>
        public IEnumerable<IOption> Options
        {
            get { return SupportedSearchOptions; }
        }

        /// <summary>
        /// Changes an option value.
        /// </summary>
        /// <param name="option">Option to be changed.</param>
        /// <param name="value">New value of the option.</param>
        public void SetOptionValue( IOption option, object value )
        {
            if( option == Interfaces.SearchOptions.CaseSensitive )
                CaseSensitive = (bool) value;
        }

        /// <summary>
        /// Returns an option value.
        /// </summary>
        /// <param name="option">Option, whose value should be retrieved.</param>
        /// <returns>Value of the option.</returns>
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
            get
            {
                return mCaseSensitive;
            }
            set
            {
                if( mCaseSensitive != value )
                {
                    mCaseSensitive = value;
                    NotifyOptionChanged( Interfaces.SearchOptions.CaseSensitive );
                }
            }
        }

        /// <summary>
        /// OptionChanged event.
        /// </summary>
        public event OptionChangedEvent OptionChanged;

        /// <summary>
        /// Notifies of option changes.
        /// </summary>
        /// <param name="option">Option, which changed.</param>
        protected virtual void NotifyOptionChanged( IOption option )
        {
            if( OptionChanged != null )
                OptionChanged( this, option );
        }
    }
}
