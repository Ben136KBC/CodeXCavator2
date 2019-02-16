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
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.Engine.Tokenizers
{
    /// <summary>
    /// RegexTokenizerPattern class.
    /// 
    /// The RegexTokenizerPattern class stores information about a regular expression based tokenizer pattern.
    /// </summary>
    /// <remarks>This tokenizer does not return line number and column information.</remarks>
    public class RegexTokenizerPattern
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RegexTokenizerPattern()
        {
            TokenType = string.Empty;
            Regex = string.Empty;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="tokenType">Type of the token matched by the regular expression.</param>
        /// <param name="regEx">Regular expression pattern.</param>
        public RegexTokenizerPattern( string tokenType, string regEx )
        {
            TokenType = tokenType;
            Regex = regEx;
        }

        /// <summary>
        /// Type of the token matched by the regex pattern.
        /// </summary>
        public string TokenType { get; set; }
        /// <summary>
        /// Regular expression pattern.
        /// </summary>
        public string Regex { get; set; }
    }

    /// <summary>
    /// Regex tokenizer configuration class.
    /// </summary>
    public class RegexTokenizerConfiguration
    {
        /// <summary>
        /// List of highlighter patterns
        /// </summary>
        public RegexTokenizerPattern[] Patterns { get; set; }
        /// <summary>
        /// Determines, whether the patters are case sensitive or not.
        /// </summary>
        public bool CaseSensitive { get; set; }
    }

    /// <summary>
    /// RegExTokenizer class.
    /// 
    /// The RegEx tokenizer uses a list regular expression patterns in order to find tokens in textual input.
    /// </summary>
    public class RegexTokenizer : ITokenizer, IConfigurable< RegexTokenizerConfiguration > 
    {
        private bool mCaseSensitive;
        private RegexTokenizerPattern[] mPatterns;
        private System.Text.RegularExpressions.Regex mPatternRegEx;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RegexTokenizer()
        {
            mCaseSensitive = true;
            Initialize();
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="patterns">List of RegexTokenizerPattern instances.</param>
        public RegexTokenizer( params RegexTokenizerPattern[] patterns )
        {
            mCaseSensitive = true;
            mPatterns = patterns ?? new RegexTokenizerPattern[] {};
            Initialize();
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="caseSensitive">Determines, whether case sensitive match should be used, or not.</param>
        /// <param name="patterns">List of RegexTokenizerPattern instances.</param>
        public RegexTokenizer( bool caseSensitive, params RegexTokenizerPattern[] patterns )
        {
            mCaseSensitive = caseSensitive;
            mPatterns = patterns ?? new RegexTokenizerPattern[] {} ;
            Initialize();
        }

        /// <summary>
        /// Configures the highlighter.
        /// </summary>
        /// <param name="configuration">Highlighter configuration.</param>
        public void Configure( RegexTokenizerConfiguration configuration )
        {
            if( configuration == null )
            {
                mPatterns = null;
                mCaseSensitive = true;
                Initialize();
            }
            else
            {
                mPatterns = configuration.Patterns ?? new RegexTokenizerPattern[] { };
                mCaseSensitive = configuration.CaseSensitive;
                Initialize();
            }
        }

        /// <summary>
        /// List of RegExTokenizerPatterns used by the highlighter.
        /// 
        /// This also defines a matchning precendence, i.e. patterns with a lower list index, have
        /// higher precendence, when being matched.
        /// </summary>
        public IEnumerable<RegexTokenizerPattern> Patterns
        {
            get
            {
                return mPatterns;
            }
            set
            {
                mPatterns = value != null ? value.ToArray() : new RegexTokenizerPattern[] { };
                Initialize();
            }
        }

        /// <summary>
        /// Determines, whether the regex patterns are case sensitive.
        /// </summary>
        public bool CaseSensitive
        {
            get
            {
                return mCaseSensitive;
            }
            set
            {
                mCaseSensitive = value;
                Initialize();
            }
        }

        /// <summary>
        /// Initializes the tokenizer.
        /// </summary>
        private void Initialize()
        {
            mPatternRegEx = CreateRegExFromPatterns( mCaseSensitive, mPatterns );
        }

        /// <summary>
        /// Creates a single regular expression from the pattern list.
        /// </summary>
        /// <param name="caseSensitive">Determines whether case-sensitive match should be used, or not.</param>
        /// <param name="patterns">List of RegexHighlighterPattern elements.</param>
        /// <returns>Single RegEx instance, matching all patterns as capturing groups.</returns>
        private static System.Text.RegularExpressions.Regex CreateRegExFromPatterns( bool caseSensitive, RegexTokenizerPattern[] patterns )
        {
            StringBuilder regExPattern = new StringBuilder();
            for( int i = 0 ; i < patterns.Length ; ++i )
            {
                regExPattern.Append( "(?<" );
                regExPattern.Append( patterns[i].TokenType );
                regExPattern.Append( ">" );
                regExPattern.Append( patterns[i].Regex );
                regExPattern.Append( ")" );
                if( i < patterns.Length - 1 )
                    regExPattern.Append( "|" );

            }
            return new System.Text.RegularExpressions.Regex( regExPattern.ToString(), System.Text.RegularExpressions.RegexOptions.ExplicitCapture | System.Text.RegularExpressions.RegexOptions.Multiline | ( !caseSensitive ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None ) );
        }

        /// <summary>
        /// Searches for regex patterns in the textual input and returns them as a list of tokens.
        /// </summary>
        /// <remarks>This tokenizer does not return line number and column information.</remarks>
        /// <param name="inputStream">Input stream containing textual data, which should be searched for regex patterns.</param>
        /// <returns>Enumeration of tokens, matching the specified regex patterns.</returns>
        public IEnumerable<IToken> EnumerateTokens( System.IO.Stream inputStream )
        {
            if( inputStream == null || !inputStream.CanRead )
                return null;
            using( StreamReader reader = new StreamReader( inputStream, Encoding.Default, true ) )
            {
                return EnumerateTokens( reader );
            }            
        }

        /// <summary>
        /// Searches for regex patterns in the textual input and returns them as a list of tokens.
        /// </summary>
        /// <remarks>This tokenizer does not return line number and column information.</remarks>
        /// <param name="inputStream">Input stream containing textual data, which should be searched for regex patterns.</param>
        /// <returns>Enumeration of tokens, matching the specified regex patterns.</returns>
        public IEnumerable<IToken> EnumerateTokens( System.IO.TextReader inputReader )
        {
            if( inputReader == null )
                yield break;

            string inputText = inputReader.ReadToEnd();
            var matches = mPatternRegEx.Matches( inputText );
            // Perform regex matching
            foreach( System.Text.RegularExpressions.Match match in matches )
            {
                // Find first group, containing a match
                for( int i = 1 ; i < match.Groups.Count ; ++i )
                {
                    var currentGroup = match.Groups[i];
                    if( currentGroup.Success )
                    {
                        string token = currentGroup.Value;
                        string tokenType = mPatternRegEx.GroupNameFromNumber(i);
                        yield return new Token( token, tokenType, currentGroup.Index );
                    }
                }
            }
        }

    }
}
