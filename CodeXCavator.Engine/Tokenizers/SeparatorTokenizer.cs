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
    /// Separator tokenizer configuration class.
    /// </summary>
    /// <remarks>This tokenizer does not return line number and column information.</remarks>
    public class SeparatorTokenizerConfiguration
    {
        /// <summary>
        /// Separator character list.
        /// </summary>
        public string Separators { get; set; }
        /// <summary>
        /// Determines, whether separator characters should also be emitted as tokens.
        /// </summary>
        public bool EmitSeparatorsAsTokens { get; set; }
        /// <summary>
        /// Determines, whether tokens should be trimmed, i.e. whether leading and trailing white space should be stripped.
        /// </summary>
        public bool TrimTokens { get; set; }
    }

    /// <summary>
    /// Separator list tokenizer.
    /// 
    /// This tokenizer splits the input text into tokens by using given list of separator characters as token delimiters.
    /// </summary>
    public class SeparatorTokenizer : ITokenizer, IConfigurable< SeparatorTokenizerConfiguration >
    {
        /// <summary>
        /// Parser states.
        /// </summary>
        private enum ParserState
        {
            Init,
            Token,
        }

        /// <summary>
        /// Token type : non whitespace
        /// </summary>
        private const string TOKEN_TYPE_NONWHITESPACE = "NonWhitespaceToken";
        /// <summary>
        /// Token type : separator
        /// </summary>
        private const string TOKEN_TYPE_SEPARATOR = "Separator";

        /// <summary>
        /// Constructor.
        /// </summary>
        public SeparatorTokenizer()
        {
            EmitSeparatorsAsTokens = true;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="separators">List of separator characters, which should be used as token delimiters</param>
        public SeparatorTokenizer( params char[] separators ) : this()
        {
            Separators = separators;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="emitSeparatorsAsTokens">Determines, whether separator characters should als be emitted as tokens.</param>
        /// <param name="trimTokens">Determines, whether tokens, should be trimmed.</param>
        /// <param name="separators">List of separator characters, which should be used as token delimiters</param>
        public SeparatorTokenizer( bool emitSeparatorsAsTokens, bool trimTokens, params char[] separators )
        {
            Separators = separators;
            EmitSeparatorsAsTokens = emitSeparatorsAsTokens;
        }

        /// <summary>
        /// Configures the tokenizer.
        /// </summary>
        /// <param name="configuration">Tokenizer configuration.</param>
        public void Configure( SeparatorTokenizerConfiguration configuration )
        {
            if( configuration != null )
            {
                Separators = configuration.Separators.ToArray() ?? new char[] { };
                EmitSeparatorsAsTokens = configuration.EmitSeparatorsAsTokens;
                TrimTokens = configuration.TrimTokens;
            }
            else
            {
                Separators = new char[] { };
                EmitSeparatorsAsTokens = true;
                TrimTokens = true;
            }
        }

        private HashSet<char> mSeparators;

        /// <summary>
        /// Separator characters.
        /// </summary>
        public char[] Separators
        {
            get
            {
                return mSeparators.ToArray();
            }
            set
            {
                mSeparators = new HashSet<char>( value );
            }
        }

        /// <summary>
        /// Determines, whether separators, should be emitted as tokens.
        /// </summary>
        public bool EmitSeparatorsAsTokens
        {
            get;
            set;
        }

        /// <summary>
        /// Determines, whether tokens, should be trimmed.
        /// </summary>
        public bool TrimTokens
        {
            get;
            set;
        }

        /// <summary>
        /// Checks, whether the specified character is a separator.
        /// </summary>
        /// <param name="c">Character, which should be checked.</param>
        /// <returns>True, if the character is contained in the list of valid separator characters, false, otherwise.</returns>
        private bool IsSeparator( char c )
        {
            return mSeparators != null && mSeparators.Contains( c );
        }

        /// <summary>
        /// Reads text from the input stream, and splits it into tokens, using whitespace and a list
        /// of separator characters as token delimiters.
        /// </summary>
        /// <remarks>This tokenizer does not return line number and column information.</remarks>
        /// <param name="inputStream">Input stream containing textual data.</param>
        /// <returns>List of non-whitespace, or separator tokens.</returns>
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
        /// Reads text from the input stream, and splits it into tokens, using whitespace and a list
        /// of separator characters as token delimiters.
        /// </summary>
        /// <remarks>This tokenizer does not return line number and column information.</remarks>
        /// <param name="inputReader">Input text reader containing textual data.</param>
        /// <returns>List of non-whitespace, or separator tokens.</returns>
        public IEnumerable<IToken> EnumerateTokens( System.IO.TextReader inputReader )
        {
            if( inputReader == null )
                yield break;

            // Initialize parser state.
            ParserState currentState = ParserState.Init;

            int tokenStart = -1;
            int position = 0;

            StringBuilder tokenBuilder = new StringBuilder( 1024 );

            // Read all characters, one by one.
            for( int c = inputReader.Read() ; c >= 0 ; c = inputReader.Read() )
            {
                char currentChar = (char) c;
                switch( currentState )
                {
                    case ParserState.Init:
                        {
                            currentState = ParserState.Token;
                            if( IsSeparator( currentChar ) )
                            {
                                if( EmitSeparatorsAsTokens )
                                    yield return new Token( new string( currentChar, 1 ), TOKEN_TYPE_SEPARATOR, position, 1 );
                                tokenStart = position + 1;
                            }
                            else
                            {
                                tokenBuilder.Append( currentChar );
                                tokenStart = position;
                            }
                        }
                        break;
                    case ParserState.Token:
                        {
                            if( IsSeparator( currentChar ) )
                            {
                                if( tokenBuilder.Length > 0 )
                                {
                                    string originalToken = tokenBuilder.ToString();
                                    if( TrimTokens )
                                    {
                                        string trimmedToken = tokenBuilder.ToString().TrimStart();
                                        int trimOffset = originalToken.Length - trimmedToken.Length;
                                        trimmedToken = trimmedToken.TrimEnd();
                                        yield return new Token( trimmedToken, TOKEN_TYPE_NONWHITESPACE, tokenStart + trimOffset, trimmedToken.Length );
                                    }
                                    else
                                    {
                                        yield return new Token( originalToken, TOKEN_TYPE_NONWHITESPACE, tokenStart, position - tokenStart );
                                    }
                                }
                                if( EmitSeparatorsAsTokens )
                                    yield return new Token( new String( currentChar, 1 ), TOKEN_TYPE_SEPARATOR, position, 1 );
                                tokenBuilder.Clear();
                                tokenBuilder.EnsureCapacity( 1024 );
                                tokenStart = position + 1;
                            }
                            else
                            {
                                tokenBuilder.Append( currentChar );
                            }
                        }
                        break;
                }
                ++position;
            }
            if( tokenStart >= 0 && tokenBuilder.Length > 0 )
            {
                yield return new Token( tokenBuilder.ToString(), TOKEN_TYPE_NONWHITESPACE, tokenStart, position - tokenStart );
            }
        }

    }

}
