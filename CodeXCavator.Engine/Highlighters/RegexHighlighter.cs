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
using System.Xml.Serialization;
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.Engine.Highlighters
{
    /// <summary>
    /// RegexHighlighterPattern class.
    /// 
    /// The RegexHighlighterPattern class stores information about a regular expression based highlighter pattern.
    /// </summary>
    [XmlRoot("Pattern")]
    public class RegexHighlighterPattern
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public RegexHighlighterPattern()
        {
            TokenType = string.Empty;
            Regex = string.Empty;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="tokenType">Type of the token matched by the regular expression.</param>
        /// <param name="regEx">Regular expression pattern.</param>
        /// <param name="color">Color, with which recognized tokens should be highlighted.</param>
        public RegexHighlighterPattern( string tokenType, string regEx, uint color )
        {
            TokenType = tokenType;
            Regex = regEx;
            Color = color;
        }

        /// <summary>
        /// Type of the token matched by the regex pattern.
        /// </summary>
        [XmlAttribute("Token")]
        public string TokenType { get; set; }
        /// <summary>
        /// Regular expression pattern.
        /// </summary>
        [XmlAttribute("RegEx")]
        public string Regex { get; set; }
        /// <summary>
        /// Color, with which recognized tokens should be highlighted.
        /// </summary>
        [XmlIgnore]
        public uint Color { get; set; }

        [XmlAttribute( "Color" )]
        public string HexColor
        {
            get { return string.Format( "#{0:X8}", Color ); }
            set { Color = uint.Parse( value.TrimStart( '#' ), System.Globalization.NumberStyles.HexNumber ); }
        }
    }

    /// <summary>
    /// Regex highlighter configuration class.
    /// </summary>
    public class RegexHighlighterConfiguration
    {
        /// <summary>
        /// List of highlighter patterns
        /// </summary>
        [XmlArray("Patterns")]
        [XmlArrayItem("Pattern")]
        public RegexHighlighterPattern[] Patterns { get; set; }
        /// <summary>
        /// Determines, whether the patters are case sensitive or not.
        /// </summary>
        [XmlAttribute]
        public bool CaseSensitive { get; set; }
    }

    /// <summary>
    /// RegexHighlighter class.
    /// 
    /// The RegexHighlighter class implements the IHighlighter interface and
    /// provides text highlighting based on regular expression patterns.
    /// 
    /// It can be initialized with a list of token types, the corresponding regular expression patterns matching the token types and the colors to be used for highlighting.
    /// 
    /// Some of the token types are treated in a special way by the highlighter:
    /// 
    /// * COMMENT_START / COMMENT_END or CommentStart / CommentEnd:
    /// If this tokens are contained in the list, the corresponding regex patterns define the start and end sequence for a multiline comment.
    /// If the highlighter finds a start sequence, it highlights everything with the color specified with the start sequence pattern until 
    /// the end sequence token is found in the text.
    /// 
    /// * IDENTIFIER / KEYWORD:
    /// Typically language keywords and identifiers may overlap, i.e. the regex for a valid C#/C++ identifier would also match language keywords.
    /// Furthermore language keywords may be found inside identifiers. 
    /// Thus in this case the highlighter tries to resolve the ambiguity, i.e. if an identifier is matched and a keyword is matched, 
    /// the keyword highlight color is only used, if the matched keyword is not only contained in the matched identifier, 
    /// but is equal to it. Otherwise the identifier color is used.
    /// </summary>
    public class RegexHighlighter : IHighlighter, IConfigurable< RegexHighlighterConfiguration >
    {
        private RegexHighlighterPattern[] mPatterns;
        private System.Text.RegularExpressions.Regex mPatternRegEx;
        private System.Text.RegularExpressions.Regex mKeywordRegEx;
        private System.Text.RegularExpressions.Regex mMultiLineBlockRegEx;
        private System.Text.RegularExpressions.Regex mTagRegEx;
        private ILookup<string, RegexHighlighterPattern> mTokenTypeToPattern;
        bool mCaseSensitive;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RegexHighlighter()
        {
            mCaseSensitive = true;
            mPatterns = new RegexHighlighterPattern[] { };
            Initialize();
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="patterns">List of RegExHighlighterPattern instances.</param>
        public RegexHighlighter( params RegexHighlighterPattern[] patterns )
        {
            mCaseSensitive = true;
            mPatterns = patterns ?? new RegexHighlighterPattern[] {};
            Initialize();
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="caseSensitive">Determines, whether case sensitive match should be used, or not.</param>
        /// <param name="patterns">List of RegExHighlighterPattern instances.</param>
        public RegexHighlighter( bool caseSensitive, params RegexHighlighterPattern[] patterns )
        {
            mCaseSensitive = caseSensitive;
            Patterns = patterns ?? new RegexHighlighterPattern[] { };
            Initialize();
        }

        /// <summary>
        /// Configures the highlighter.
        /// </summary>
        /// <param name="configuration">Highlighter configuration.</param>
        public void Configure( RegexHighlighterConfiguration configuration )
        {
            if( configuration == null )
            {
                mPatterns = null;
                mCaseSensitive = true;
                Initialize();
            }
            else
            {
                mPatterns = configuration.Patterns ?? new RegexHighlighterPattern[] {};
                mCaseSensitive = configuration.CaseSensitive;
                Initialize();
            }            
        }

        /// <summary>
        /// List of RegExHighlighterPatterns used by the highlighter.
        /// 
        /// This also defines a matchning precendence, i.e. patterns with a lower list index, have
        /// higher precendence, when being matched.
        /// </summary>
        public IEnumerable<RegexHighlighterPattern> Patterns
        {
            get
            {
                return mPatterns;
            }
            set
            {
                mPatterns = value != null ? value.ToArray() : new RegexHighlighterPattern[] {};
                Initialize();
            }
        }

        /// <summary>
        /// Determines, whether the regular expression patterns are case sensitive.
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
        /// Initializes the highlighter.
        /// </summary>
        private void Initialize()
        {
            mPatternRegEx = CreateRegExFromPatterns( mCaseSensitive, mPatterns );
            mKeywordRegEx = CreateKeywordRegExFromPatterns( mCaseSensitive, mPatterns );
            mMultiLineBlockRegEx = CreateMultiLineBlockRegExFromPatterns( mCaseSensitive, mPatterns );
            mTagRegEx = CreateTagRegExFromPatterns( mCaseSensitive, mPatterns );
            mTokenTypeToPattern = mPatterns.ToLookup( pattern => pattern.TokenType, StringComparer.OrdinalIgnoreCase );
        }

        /// <summary>
        /// Creates a single regular expression from the pattern list.
        /// </summary>
        /// <param name="caseSensitive">Determines whether case-sensitive match should be used, or not.</param>
        /// <param name="patterns">List of RegexHighlighterPattern elements.</param>
        /// <returns>Single RegEx instance, matching all patterns as capturing groups.</returns>
        private static System.Text.RegularExpressions.Regex CreateRegExFromPatterns( bool caseSensitive, RegexHighlighterPattern[] patterns )
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
        /// Creates a single keyword matching regular expression from the pattern list.
        /// </summary>
        /// <param name="caseSensitive">Determines whether case-sensitive match should be used, or not.</param>
        /// <param name="patterns">List of RegexHighlighterPattern elements.</param>
        /// <returns>Single RegEx instance, matching all keyword patterns as capturing groups.</returns>
        private static System.Text.RegularExpressions.Regex CreateKeywordRegExFromPatterns( bool caseSensitive, RegexHighlighterPattern[] mPatterns )
        {
            StringBuilder regExPattern = new StringBuilder();
            bool keywordPatternFound = false;
            for( int i = 0 ; i < mPatterns.Length ; ++i )
            {
                if( mPatterns[i].TokenType.IndexOf( "KEYWORD", StringComparison.OrdinalIgnoreCase ) != 0 )
                    continue;
                if( keywordPatternFound )
                    regExPattern.Append( "|" );
                keywordPatternFound = true;
                regExPattern.Append( "(?<" );
                regExPattern.Append( mPatterns[i].TokenType );
                regExPattern.Append( ">" );
                regExPattern.Append( mPatterns[i].Regex );
                regExPattern.Append( ")" );
            }
            return keywordPatternFound ? new System.Text.RegularExpressions.Regex( regExPattern.ToString(), System.Text.RegularExpressions.RegexOptions.ExplicitCapture | System.Text.RegularExpressions.RegexOptions.Multiline | ( !caseSensitive ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None ) ) : null;
        }

        /// <summary>
        /// Creates a multi line block matching regular expression from the pattern list.
        /// </summary>
        /// <param name="caseSensitive">Determines whether case-sensitive match should be used, or not.</param>
        /// <param name="patterns">List of RegexHighlighterPattern elements.</param>
        /// <returns>Single RegEx instance, matching all multi line blocks tokens as capturing groups.</returns>
        private static System.Text.RegularExpressions.Regex CreateMultiLineBlockRegExFromPatterns( bool caseSensitive, RegexHighlighterPattern[] mPatterns )
        {
            StringBuilder regExPattern = new StringBuilder();
            bool multiLineBlocksPatternsFound = false;
            for( int i = 0 ; i < mPatterns.Length ; ++i )
            {
                string tokenType = mPatterns[i].TokenType;
                if( !( tokenType.Equals( "COMMENT_START", StringComparison.OrdinalIgnoreCase ) || tokenType.Equals( "CommentStart", StringComparison.OrdinalIgnoreCase ) ) && 
                    !( tokenType.Equals( "COMMENT_END", StringComparison.OrdinalIgnoreCase ) || tokenType.Equals( "CommentEnd", StringComparison.OrdinalIgnoreCase ) ) )
                    continue;                    
                if( multiLineBlocksPatternsFound )
                    regExPattern.Append( "|" );
                multiLineBlocksPatternsFound = true;
                regExPattern.Append( "(?<" );
                regExPattern.Append( mPatterns[i].TokenType );
                regExPattern.Append( ">" );
                regExPattern.Append( mPatterns[i].Regex );
                regExPattern.Append( ")" );
            }
            return multiLineBlocksPatternsFound ? new System.Text.RegularExpressions.Regex( regExPattern.ToString(), System.Text.RegularExpressions.RegexOptions.ExplicitCapture | System.Text.RegularExpressions.RegexOptions.Multiline | ( !caseSensitive ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None ) ) : null;
        }

        /// <summary>
        /// Creates a single tag matching regular expression from the pattern list.
        /// </summary>
        /// <param name="caseSensitive">Determines whether case-sensitive match should be used, or not.</param>
        /// <param name="patterns">List of RegexHighlighterPattern elements.</param>
        /// <returns>Single RegEx instance, matching all tag patterns as capturing groups.</returns>
        private static System.Text.RegularExpressions.Regex CreateTagRegExFromPatterns( bool caseSensitive, RegexHighlighterPattern[] mPatterns )
        {
            StringBuilder regExPattern = new StringBuilder();
            bool keywordPatternFound = false;
            for( int i = 0 ; i < mPatterns.Length ; ++i )
            {
                if( !mPatterns[i].TokenType.Equals( "TAG", StringComparison.OrdinalIgnoreCase ) )
                    continue;
                keywordPatternFound = true;
                regExPattern.Append( "(?<" );
                regExPattern.Append( mPatterns[i].TokenType );
                regExPattern.Append( ">" );
                regExPattern.Append( mPatterns[i].Regex );
                regExPattern.Append( ")" );
                break;
            }
            return keywordPatternFound ? new System.Text.RegularExpressions.Regex( regExPattern.ToString(), System.Text.RegularExpressions.RegexOptions.ExplicitCapture | System.Text.RegularExpressions.RegexOptions.Multiline | ( !caseSensitive ? System.Text.RegularExpressions.RegexOptions.IgnoreCase : System.Text.RegularExpressions.RegexOptions.None ) ) : null;
        }

        /// <summary>
        /// Highlights elements in the text to which access is provided through the text reader.
        /// </summary>
        /// <param name="inputReader">Text reader, providing access to the text, which should be highlighted.</param>
        /// <param name="activeBlock">Active multi line block.</param>
        /// <returns>Enumeration of IHighlighterToken instances.</returns>
        public IEnumerable<Interfaces.IHighlighterToken> Highlight( System.IO.TextReader inputReader, IHighlighterToken activeBlock = null )
        {
            if( inputReader != null )
            {
                string text = inputReader.ReadToEnd();
                return Highlight( text, activeBlock );
            }
            else
            {
                return new Interfaces.HighlighterToken[] { };
            }
        }

        /// <summary>
        /// Highlights elements in the specified text.
        /// 
        /// The highlighter tries to find matches of the patterns specified by the Patterns property in the provided text.
        /// For each match a IHighlighterToken instance is returned and the color is used as defined in the matched pattern
        /// definition. Except for multi line comments an keywords/identifiers the Patterns list also defines a token type 
        /// precedence, i.e. a highlighter token instance is always returned for the first token type matched by the regex,
        /// even if mutliple token types are matched simultanously.
        /// </summary>
        /// <param name="text">Text, which should be highlighted.</param>
        /// <param name="activeBlock">Active multi line block.</param>
        /// <returns>Enumeration of IHighlighterToken instances.</returns>
        public IEnumerable<Interfaces.IHighlighterToken> Highlight( string text, IHighlighterToken activeBlock = null )
        {
            if( text != null )
            {
                bool activeBlockIsComment = activeBlock != null && activeBlock.Type.Equals( "COMMENT", StringComparison.OrdinalIgnoreCase );
                bool insideComment = activeBlock != null && activeBlock.Type.Equals( "COMMENT", StringComparison.OrdinalIgnoreCase );
                int commentStart = insideComment ? 0 : -1;
                int commentEnd = -1;
                uint commentColor = insideComment ? activeBlock.Color : 0;

                var matches = mPatternRegEx.Matches( text );
                // Perform regex matching
                foreach( System.Text.RegularExpressions.Match match in matches )
                {
                    // Find first group, containing a match
                    for( int i = 1 ; i <= match.Groups.Count ; ++i )
                    {
                        var currentGroup = match.Groups[i];
                        if( currentGroup.Success )
                        {
                            string tokenType = mPatternRegEx.GroupNameFromNumber(i);
                            // Not in multi-line comment?
                            if( !( insideComment ) ) 
                            {
                                // Special treatment for identifiers and keywords...
                                if( tokenType.Equals( "IDENTIFIER", StringComparison.OrdinalIgnoreCase ) && mKeywordRegEx != null )
                                {
                                    string identifier = currentGroup.Value;
                                    var keywordMatch = mKeywordRegEx.Match( identifier );
                                    // Keywords have precedence over identifiers, but identifiers have precendence over keywords,
                                    // if the keyword is only contained in the identifier.
                                    if( keywordMatch.Success && keywordMatch.Length == identifier.Length )
                                    {
                                        // Determine keyword token type
                                        for( int g = 1 ; g <= keywordMatch.Groups.Count ; ++g )
                                        {
                                            var currentKeywordGroup = keywordMatch.Groups[g];
                                            if( currentKeywordGroup.Success )
                                            {
                                                tokenType = mKeywordRegEx.GroupNameFromNumber( g );
                                                break;
                                            }   
                                        }
                                        yield return new Interfaces.HighlighterToken( mTokenTypeToPattern[tokenType].FirstOrDefault().Color, tokenType, currentGroup.Index, currentGroup.Index + currentGroup.Length );
                                    }
                                }
                                // Multiline comment handling...
                                if( tokenType.Equals( "COMMENT_START", StringComparison.OrdinalIgnoreCase ) || tokenType.Equals( "CommentStart", StringComparison.OrdinalIgnoreCase ) )
                                {
                                    insideComment = true;
                                    commentStart = currentGroup.Index;
                                    commentColor = mTokenTypeToPattern[ tokenType ].FirstOrDefault().Color;
                                }
                                else
                                // Single line comment handling...
                                if( tokenType.Equals( "LINE_COMMENT", StringComparison.OrdinalIgnoreCase ) || tokenType.Equals( "LineComment", StringComparison.OrdinalIgnoreCase ) )
                                {
                                    commentColor = mTokenTypeToPattern[ tokenType ].FirstOrDefault().Color;
                                    var tagColor = mTokenTypeToPattern["TAG"].FirstOrDefault().Color;

                                    // Handle tags...
                                    if( mTagRegEx != null )
                                    {
                                        commentStart = currentGroup.Index;
                                        string lineComment = currentGroup.Value;
                                        var tags = mTagRegEx.Matches( lineComment );
                                        if( tags.Count > 0 )
                                        {
                                            foreach( System.Text.RegularExpressions.Match tag in tags )
                                            {
                                                commentEnd = tag.Index + currentGroup.Index;
                                                int tagStart = tag.Index + currentGroup.Index;
                                                int tagEnd = tagStart + tag.Length;
                                                if( commentEnd > commentStart )
                                                    yield return new Interfaces.HighlighterToken( commentColor, tokenType, commentStart, commentEnd );
                                                if( tagEnd > tagStart )
                                                    yield return new Interfaces.HighlighterToken( tagColor, "TAG", tagStart, tagEnd );
                                                commentStart = tagEnd;
                                            }
                                            commentEnd = currentGroup.Index + currentGroup.Length;
                                            if( commentEnd > commentStart )
                                                yield return new Interfaces.HighlighterToken( commentColor, tokenType, commentStart, commentEnd );
                                        }
                                        else
                                        {
                                            yield return new Interfaces.HighlighterToken( mTokenTypeToPattern[tokenType].FirstOrDefault().Color, tokenType, currentGroup.Index, currentGroup.Index + currentGroup.Length );
                                        }
                                    }
                                    else
                                    {
                                        yield return new Interfaces.HighlighterToken( mTokenTypeToPattern[tokenType].FirstOrDefault().Color, tokenType, currentGroup.Index, currentGroup.Index + currentGroup.Length );
                                    }
                                }
                                else
                                // Normal match...
                                {
                                    yield return new Interfaces.HighlighterToken( mTokenTypeToPattern[tokenType].FirstOrDefault().Color, tokenType, currentGroup.Index, currentGroup.Index + currentGroup.Length );
                                }
                            }
                            else
                            {
                                // Handle tag
                                if( tokenType.Equals( "TAG", StringComparison.OrdinalIgnoreCase ) )
                                {
                                    commentEnd = currentGroup.Index;
                                    yield return new Interfaces.HighlighterToken( commentColor, "COMMENT", commentStart, commentEnd );
                                    yield return new Interfaces.HighlighterToken( mTokenTypeToPattern[tokenType].FirstOrDefault().Color, tokenType, currentGroup.Index, currentGroup.Index + currentGroup.Length );
                                    commentStart = currentGroup.Index + currentGroup.Length;
                                    insideComment = true;
                                }
                                // Multiline comment handling...
                                if( tokenType.Equals( "COMMENT_END", StringComparison.OrdinalIgnoreCase ) || tokenType.Equals( "CommentEnd", StringComparison.OrdinalIgnoreCase ) )
                                {
                                    insideComment = false;
                                    commentEnd = currentGroup.Index + currentGroup.Length;
                                    yield return new Interfaces.HighlighterToken( commentColor, "COMMENT", commentStart, commentEnd );
                                }
                            }
                            break;
                        }
                    }
                }

                // Emit comment token if not closed
                if( insideComment && commentStart >= 0 )
                    yield return new Interfaces.HighlighterToken( commentColor, "COMMENT", commentStart, text.Length );
            }
            else
            {
                yield break;
            }
        }

        /// <summary>
        /// Returns tokens for all multi line comments.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Enumeration of all multi line comment blocks.</returns>
        public IEnumerable<IHighlighterToken> HighlightMultiLineBlocks( string text )
        {
            if( mMultiLineBlockRegEx != null )
            {
                if( text != null )
                {
                    bool insideComment = false;
                    int commentStart = -1;
                    int commentEnd = -1;
                    uint commentColor = 0;
                    var matches = mMultiLineBlockRegEx.Matches( text );
                    // Perform regex matching
                    foreach( System.Text.RegularExpressions.Match match in matches )
                    {
                        // Find first group, containing a match
                        for( int i = 1 ; i < match.Groups.Count ; ++i )
                        {
                            var currentGroup = match.Groups[i];
                            if( currentGroup.Success )
                            {
                                string tokenType = mMultiLineBlockRegEx.GroupNameFromNumber( i );
                                // Not in multi-line comment?
                                if( !insideComment )
                                {
                                    // Multiline comment handling...
                                    if( tokenType.Equals( "COMMENT_START", StringComparison.OrdinalIgnoreCase ) || tokenType.Equals( "CommentStart", StringComparison.OrdinalIgnoreCase ) )
                                    {
                                        insideComment = true;
                                        commentStart = currentGroup.Index;
                                        commentColor = mTokenTypeToPattern[tokenType].FirstOrDefault().Color;
                                    }
                                }
                                else
                                {
                                    // Multiline comment handling...
                                    if( tokenType.Equals( "COMMENT_END", StringComparison.OrdinalIgnoreCase ) || tokenType.Equals( "CommentEnd", StringComparison.OrdinalIgnoreCase ) )
                                    {
                                        insideComment = false;
                                        commentEnd = currentGroup.Index + currentGroup.Length;
                                        yield return new Interfaces.HighlighterToken( commentColor, "COMMENT", commentStart, commentEnd );
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                else
                {
                    yield break;
                }
            }
            yield break;
        }

        /// <summary>
        /// Returns tokens for all multi line comments.
        /// </summary>
        /// <param name="text"></param>
        /// <returns>Enumeration of all multi line comment blocks.</returns>
        public IEnumerable<IHighlighterToken> HighlightMultiLineBlocks( System.IO.TextReader inputReader )
        {
            if( inputReader != null )
            {
                string text = inputReader.ReadToEnd();
                return HighlightMultiLineBlocks( text );
            }
            else
            {
                return new Interfaces.HighlighterToken[] { };
            }
        }
    }
}
