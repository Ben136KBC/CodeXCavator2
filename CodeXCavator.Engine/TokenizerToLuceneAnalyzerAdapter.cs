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
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using AttributeSource = Lucene.Net.Util.AttributeSource;

namespace CodeXCavator.Engine
{
    internal enum Case
    {
        Insensitive = 0,
        Sensitive = 1
    };

    /// <summary>
    /// Lucene analyzer / tokenizer related extension functions.
    /// </summary>
    internal static class LuceneAnalyzerExtensions
    {
        /// <summary>
        /// Convertes a CodeXCavator tokenizer into a Lucene Analyzer.
        /// </summary>
        /// <param name="tokenizer">Tokenizer, which should be converted to an analyzer.</param>
        /// <param name="caseMode">Defines, how the analyzer should treat character case.</param>
        /// <returns>Analyzer created from the given tokenizer.</returns>
        public static Analyzer ToAnalyzer( this ITokenizer tokenizer, Case caseMode = Case.Sensitive )
        {
            if( tokenizer != null )
                return new TokenizerToLuceneAnalyzerAdapter( tokenizer, caseMode );
            return null;
        }
    }
    /// <summary>
    /// TokenizerToLuceneAnalyzerAdapter class.
    /// 
    /// The TokenizerToLuceneAnalyzerAdapter derives from the abstract the Lucene.Net.Analysis.Analyzer class
    /// and acts as an adapter between ITokenizer and Analyzer, i.e. it allows to use
    /// CodeXCavator tokenizers together with Lucene.
    /// </summary>
    internal class TokenizerToLuceneAnalyzerAdapter : Analyzer
    {
        /// <summary>
        /// ITokenizerTokenStream class.
        /// 
        /// The ITokenizerTokenStream derives from the abstract Tokenizer class, and
        /// wraps an CodeXCavator ITokenizer into a Lucene Tokenizer.
        /// </summary>
        internal class ITokenizerTokenStream : Tokenizer
        {
            private ITokenizer mTokenizer;
            private IEnumerator<IToken> mTokenEnumerator;
            private Case mCaseMode;

            private readonly ITermAttribute mTermAttribute;
            private readonly IOffsetAttribute mOffsetAttribute;

            /// <summary>
            /// Initialization constructor.
            /// </summary>
            /// <param name="tokenizer">CodeXCavator ITokenizer instance.</param>
            /// <param name="reader">Text reader, providing access to the text, from which tokens should be extracted.</param>
            /// <param name="caseMode">Defines, how the token stream should treat character case.</param>
            internal ITokenizerTokenStream( ITokenizer tokenizer, System.IO.TextReader reader, Case caseMode = Case.Sensitive ) : base( reader )
            {
                mTermAttribute = AddAttribute<ITermAttribute>();
                mOffsetAttribute = AddAttribute<IOffsetAttribute>();
                mTokenizer = tokenizer;
                mCaseMode = caseMode;
            }

            public override void Reset()
            {
                base.Reset();
                mTokenEnumerator = null;
            }

            public override void Reset( System.IO.TextReader input )
            {
                base.Reset( input );
                mTokenEnumerator = null;
            }

            public override bool IncrementToken()            
            {
                if( mTokenEnumerator == null && base.input == null )
                    return false;
                if( mTokenEnumerator == null )
                    mTokenEnumerator = mTokenizer.EnumerateTokens( base.input ).GetEnumerator();

                // Fetch tokens from CodeXCavator tokenizer
                bool next = mTokenEnumerator.MoveNext();
                if( next )
                {
                    ClearAttributes();
                    var token = mTokenEnumerator.Current;
                    // Set term attribute to token text.
                    mTermAttribute.SetTermBuffer( mCaseMode == Case.Sensitive ? token.Text : token.Text.ToLowerInvariant() );
                    // Set offset attribute to token position
                    mOffsetAttribute.SetOffset( token.Position, token.Position + token.Length );
                }

                return next;
            }
        }

        private Case mCaseMode;
        private ITokenizer mTokenizer;

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="tokenizer">Tokenizer, which should be wrapped into a Lucene analyzer.</param>
        /// <param name="caseMode">Controls, how the analyzer should treat character case.</param>
        internal TokenizerToLuceneAnalyzerAdapter( ITokenizer tokenizer, Case caseMode = Case.Sensitive )
        {
            mTokenizer = tokenizer;        
            mCaseMode = caseMode;
        }

        /// <summary>
        /// Returns a Lucene TokenStream, which enumerates the tokens
        /// by using the wrapped CodeXCavator tokenizer.
        /// </summary>
        /// <param name="fieldName">Name of the document field, for which tokens should be generated.</param>
        /// <param name="reader">Text reader providing access to the content of the document field.</param>
        /// <returns>TokenStream instance.</returns>
        public override TokenStream TokenStream( string fieldName, System.IO.TextReader reader )
        {
            return new ITokenizerTokenStream( mTokenizer, reader, mCaseMode );
        }

        /// <summary>
        /// Returns a resuable token stream, which enumerates the tokens
        /// by using the wrapped CodeXCavator tokenizer.
        /// </summary>
        /// <param name="fieldName">Name of the document field, for which tokens should be generated.</param>
        /// <param name="reader">Text reader providing access to the content of the document field.</param>
        /// <returns>TokenStream instance.</returns>
        public override TokenStream ReusableTokenStream( System.String fieldName, System.IO.TextReader reader )
        {
            var tokenizer = (Tokenizer) PreviousTokenStream;
            if( tokenizer == null )
            {
                tokenizer = new ITokenizerTokenStream( mTokenizer, reader, mCaseMode );
                PreviousTokenStream = tokenizer;
            }
            else
                tokenizer.Reset( reader );
            return tokenizer;
        }
    }
}
