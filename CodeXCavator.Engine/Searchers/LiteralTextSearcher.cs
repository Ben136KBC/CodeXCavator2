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
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace CodeXCavator.Engine.Searchers
{
    /// <summary>
    /// Literal text searcher.
    /// 
    /// The LiteralTextSearcher searches for string literals.
    /// </summary>
    public class LiteralTextSearcher : ICaptionProvider, IDescriptionProvider, IIconProvider, ITextSearcher
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public LiteralTextSearcher()
        {
            mCaseSensitive = false;
            mWordWise = false;
        }

        /// <summary>
        /// Caption of the text searcher.
        /// </summary>
        string ICaptionProvider.Caption
        {
            get { return "Search literal"; }
        }

        /// <summary>
        /// Description of the text searcher.
        /// </summary>
        string IDescriptionProvider.Description
        {
            get { return "Searches for text occurrences matching a literal string."; }
        }

        private static ImageSource mIcon = new BitmapImage( new Uri( "/CodeXCavator.Engine;component/Images/find_literal.png", UriKind.RelativeOrAbsolute ) );

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
            return true;
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
            var searchMode = CaseSensitive ? StringComparison.Ordinal : StringComparison.CurrentCultureIgnoreCase;

            int lastPos = 0;
            int lineNumber = 0;
            int lineOffset = 0;
            for( int currentPos = text.IndexOf( searchQuery, searchMode ) ; currentPos >= 0 ; currentPos = text.IndexOf( searchQuery, lastPos, searchMode ) )
            {
                var lineCountAndLastLineOffset = TextUtilities.CountLinesInRangeAndGetOffsetOfLastFoundLine( text, lastPos, currentPos );
                if( lineCountAndLastLineOffset.NumberOfLines > 0 )
                {
                    lineNumber += lineCountAndLastLineOffset.NumberOfLines;
                    lineOffset = lineCountAndLastLineOffset.LastLineOffset;
                }
                lastPos = currentPos + searchQuery.Length;

                if( WordWise && !TextUtilities.IsMatchWholeWord( text, currentPos, searchQuery.Length ) )
                    continue;

                yield return new Occurrence( searchQuery, lineNumber, currentPos - lineOffset, null );
            }
        }

        /// <summary>
        /// List of supported search options.
        /// </summary>
        public static readonly IEnumerable<IOption> SupportedSearchOptions = new IOption[] { SearchOptions.CaseSensitive, SearchOptions.WordWise };

        /// <summary>
        /// List of supported options.
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
            if( option == Interfaces.SearchOptions.WordWise )
                WordWise = (bool) value;
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
            if( option == Interfaces.SearchOptions.WordWise )
                return WordWise;
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

        private bool mWordWise;

        /// <summary>
        /// Controls, whether search should be word wise or not.
        /// </summary>
        public bool WordWise 
        {
            get
            {
                return mWordWise;
            }
            set
            {
                if( mWordWise != value )
                {
                    mWordWise = value;
                    NotifyOptionChanged( Interfaces.SearchOptions.WordWise );
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
