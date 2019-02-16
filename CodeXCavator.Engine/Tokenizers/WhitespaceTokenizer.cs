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
    /// Whitespace tokenizer class.
    /// 
    /// The whitespace tokenizer splits a text into tokens. Whitespace is used as
    /// a delimiter in order to distinguish tokens.
    /// </summary>
    /// <remarks>This tokenizer does not return line number and column information.</remarks>
    public class WhitespaceTokenizer : ITokenizer
    {
        /// <summary>
        /// Parser states.
        /// </summary>
        private enum ParserState
        {
            Init,
            WhiteSpace,
            NonWhiteSpace,
        }

        /// <summary>
        /// Token type: Non whitespace
        /// </summary>
        private const string TOKEN_TYPE_NONWHITESPACE = "NonWhitespaceToken";

        /// <summary>
        /// Enumerates non whitespace tokens by reading from the given input stream.
        /// </summary>
        /// <remarks>This tokenizer does not return line number and column information.</remarks>
        /// <param name="inputStream">Input stream containing textual data.</param>
        /// <returns>Enumeration of non-whitespace tokens.</returns>
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
        /// Enumerates non whitespace tokens by using the given test reader.
        /// </summary>
        /// <remarks>This tokenizer does not return line number and column information.</remarks>
        /// <param name="inputReader">Input reader containing textual data.</param>
        /// <returns>Enumeration of non-whitespace tokens.</returns>
        public IEnumerable<IToken> EnumerateTokens( System.IO.TextReader inputReader )
        {
            if( inputReader == null )
                yield break;

            // Initialize parser
            ParserState currentState = ParserState.Init;

            int tokenStart = -1;
            int position = 0;

            StringBuilder tokenBuilder = new StringBuilder( 1024 );

            // Read all characters, one by one
            for( int c = inputReader.Read() ; c >= 0 ; c= inputReader.Read() )
            {
                char currentChar = (char) c;
                switch( currentState )
                {
                    case ParserState.Init:
                        {
                            if( char.IsWhiteSpace( currentChar ) || currentChar == '\n' || currentChar == '\r' )
                            {
                                currentState = ParserState.WhiteSpace;
                            }
                            else
                            {
                                currentState = ParserState.NonWhiteSpace;
                                tokenBuilder.Append( currentChar );
                                tokenStart = position;
                            }
                        }
                        break;
                    case ParserState.WhiteSpace:
                        {
                            if( !( char.IsWhiteSpace( currentChar ) || currentChar == '\n' || currentChar == '\r' ) )
                            {
                                currentState = ParserState.NonWhiteSpace;
                                tokenBuilder.Clear();
                                tokenBuilder.EnsureCapacity( 1024 );
                                tokenBuilder.Append( currentChar );
                                tokenStart = position;
                            }
                        }
                        break;
                    case ParserState.NonWhiteSpace:
                        {
                            if( ( char.IsWhiteSpace( currentChar ) || currentChar == '\n' || currentChar == '\r' ) )
                            {
                                yield return new Token( tokenBuilder.ToString(), TOKEN_TYPE_NONWHITESPACE, tokenStart, position - tokenStart );
                                currentState = ParserState.WhiteSpace;
                                tokenBuilder.Clear();
                                tokenBuilder.EnsureCapacity( 1024 );
                                tokenStart = -1;
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
                yield return new Token( tokenBuilder.ToString(), "NonWhitespaceToken", tokenStart, position - tokenStart );
            }
        }

    }
}
