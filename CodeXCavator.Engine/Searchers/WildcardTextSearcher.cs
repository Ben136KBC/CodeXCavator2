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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;

namespace CodeXCavator.Engine.Searchers
{
    /// <summary>
    /// Wildcard text searcher.
    /// 
    /// The WildCardTextSearcher searches for occurrences matching a wildcard pattern.
    /// </summary>
    public class WildcardTextSearcher : ICaptionProvider, IDescriptionProvider, IIconProvider, ITextSearcher
    {

        /// <summary>
        /// Constructor.
        /// </summary>
        public WildcardTextSearcher()
        {
            CaseSensitive = true;
        }

        /// <summary>
        /// Caption of the text searcher.
        /// </summary>
        string ICaptionProvider.Caption
        {
            get { return "Search wildcard"; }
        }

        /// <summary>
        /// Description of the text seracher.
        /// </summary>
        string IDescriptionProvider.Description
        {
            get { return "Searches for text occurrences matching a wildcard pattern."; }
        }

        private static ImageSource mIcon = new BitmapImage( new Uri( "/CodeXCavator.Engine;component/Images/find_wildcard.png", UriKind.RelativeOrAbsolute ) );

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
                Microsoft.VisualBasic.CompilerServices.Operators.LikeString( "", searchQuery, Microsoft.VisualBasic.CompareMethod.Binary );
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
            Regex searchQueryRegex = new Regex( ConvertLikePatternToRegexPattern( searchQuery ), !CaseSensitive ? RegexOptions.IgnoreCase | RegexOptions.Multiline : RegexOptions.Multiline );

            int lastPos = 0;
            int lineNumber = 0;
            int lineOffset = 0;
            int searchPos = 0;
            for( var match = searchQueryRegex.Match( text ) ; match != null && match.Success && searchPos < text.Length ; match = searchPos < text.Length ? searchQueryRegex.Match( text, searchPos ) : null )
            {
                int currentPos = match.Index;
                var lineCountAndLastLineOffset = TextUtilities.CountLinesInRangeAndGetOffsetOfLastFoundLine( text, lastPos, currentPos );
                if( lineCountAndLastLineOffset.NumberOfLines > 0 )
                {
                    lineNumber += lineCountAndLastLineOffset.NumberOfLines;
                    lineOffset = lineCountAndLastLineOffset.LastLineOffset;
                }

                string trimmedMatch = match.Value.TrimEnd( '\n', '\r' );
                lastPos = currentPos + trimmedMatch.Length;

                searchPos = lastPos;
                while( searchPos < text.Length && "\n\r".Contains( text[searchPos] ) )
                    ++searchPos;

                if( match.Length == 0 )
                    ++searchPos;

                if( trimmedMatch.Length == 0 )
                    continue;

                if( WordWise && !TextUtilities.IsMatchWholeWord( text, currentPos, trimmedMatch.Length ) )
                    continue;

                yield return new Occurrence( trimmedMatch, lineNumber, currentPos - lineOffset, null );
            }
        }

        /// <summary>
        /// Converts a VB.NET Like pattern into a regular expression.
        /// </summary>
        /// <param name="likePattern">Like pattern to be converted.</param>
        /// <returns>Regular expression derived from the like pattern.</returns>
        private static string ConvertLikePatternToRegexPattern( string likePattern )
        {
            StringBuilder regexBuilder = new StringBuilder();

            bool inCharacterGroup = false;
            int characterGroupStart = -1;
            int characterGroupEnd = -1;

            for( int i = 0 ; i < likePattern.Length ; ++i )
            {
                var c = likePattern[i];
                if( !inCharacterGroup )
                {
                    if( c == '?' )
                        regexBuilder.Append( '.' );
                    else
                    if( c == '*' )
                        regexBuilder.Append( ".*" );
                    else
                    if( c == '#' )
                        regexBuilder.Append( @"\d" );
                    else
                    if( c == '[' )
                    {
                        inCharacterGroup = true;
                        characterGroupStart = i;
                    }
                    else
                    {
                        regexBuilder.Append( Regex.Escape( c.ToString() ) );
                    }
                }
                else
                {
                    if( c == ']' )
                    {
                        const string negatedGroupPrefix = @"[!";
                        inCharacterGroup = false;
                        characterGroupEnd = i;
                        string characterGroup = likePattern.Substring( characterGroupStart, characterGroupEnd - characterGroupStart );
                        if( characterGroup.StartsWith( negatedGroupPrefix ) )
                            characterGroup = @"[^" + characterGroup.Substring( negatedGroupPrefix.Length );
                        regexBuilder.Append( characterGroup );
                    }
                }
            }

            return regexBuilder.ToString();
        }

        /// <summary>
        /// List of supported search options.
        /// </summary>
        public static readonly IOption[] SupportedSearchOptions = { SearchOptions.CaseSensitive, SearchOptions.WordWise };

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
