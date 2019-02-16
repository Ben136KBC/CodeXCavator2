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
using System.Text.RegularExpressions;
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.Engine.Tokenizers
{
    /// <summary>
    /// TagTokenizer class.
    /// 
    /// The tag tokenizer searches textual data for tags. 
    /// 
    /// A tag is a special search keyword, which starts with a special tag begin sequence "+#" and end sequence "#+" and can also contain a list of urls.
    /// </summary>
    /// <example>
    /// +#FileIo#+
    /// 
    /// This is a simple tag.
    /// </example>
    /// <example>
    /// +#Parser#+[http://en.wikipedia.org/wiki/Parser]
    /// 
    /// This is a tag including a url.
    /// </example>
    /// <example>
    /// +#Parser#+[http://en.wikipedia.org/wiki/Parser][http://en.wikipedia.org/wiki/LL_parser]
    /// 
    /// This is a tag including multiple urls.
    /// </example>
    /// <example>
    /// +#Parser#+[http://en.wikipedia.org/wiki/Parser]<Wikipedia article: Parser>
    /// This is a tag including a url and a textual description for the url.
    /// </example>
    /// <example>
    /// +#Parser#+[http://en.wikipedia.org/wiki/Parser]<Wikipedia article: Parser>[http://en.wikipedia.org/wiki/LL_parser]<Wikipedia article: LL-Parser>
    /// This is a tag including multiple urls and a textual description for each url.
    /// </example>
    /// <example>
    /// +#ClassDef#+[xcav://class]<Class definitions>
    /// This is a tag including a url and a textual description for the url.
    /// </example>
    public class TagTokenizer : ITokenizer
    {
        /// <summary>
        /// Checks, whether the specified text is a tag.
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static bool IsTag( string text )
        {
            return mTagPatternRegex.Match( text ).Success;
        }

        /// <summary>
        /// Returns the tag from the specified text.
        /// </summary>
        /// <param name="text">Text containing a tag definition.</param>
        /// <returns>Tag, or null, if no tag definition is contained in the text.</returns>
        public static string GetTag( string text )
        {
            if( text == null )
                return null;
            var match = mTagPatternRegex.Match( text );
            if( match != null && match.Success )
                return match.Groups["TAG"].Value;
            return null;
        }

        /// <summary>
        /// TagInfo class.
        /// 
        /// Stores information about a tag.
        /// </summary>
        public class TagInfo
        {
            /// <summary>
            /// Url of the tag.
            /// </summary>
            public string Url { get; set; }
            /// <summary>
            /// Caption of the tag.
            /// </summary>
            public string Caption { get; set; }
        }

        private static readonly Regex mTagPatternRegex = new Regex( @"\+\#(?<TAG>[_a-zA-Z][a-zA-Z_0-9\.]*)\#\+(?<LINK>\[(?<URL>[^\]]*)\](\<(?<CAPTION>[^\>]*)\>)?)*", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture );
        private const string TOKEN_TYPE_TAG = "Tag";

        /// <summary>
        /// Internal delegate, which is called for each tag, which has been found.
        /// </summary>
        internal event Action<IToken> OnTag;

        /// <summary>
        /// Enumerates all tags contained in the input text.
        /// </summary>
        /// <param name="inputStream">Input stream containing textual data, which should be scanned for tags.</param>
        /// <returns>Enumeration of tag tokens. The data field of the token contains an array of TagInfo objects.</returns>
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
        /// Enumerates all tags contained in the input text.
        /// </summary>
        /// <param name="inputReader">Input reader containing textual data, which should be scanned for tags.</param>
        /// <returns>Enumeration of tag tokens. The data field of the token contains an array of TagInfo objects.</returns>
        public IEnumerable<IToken> EnumerateTokens( System.IO.TextReader inputReader )
        {
            if( inputReader == null )
                yield break;

            string inputText = inputReader.ReadToEnd();
            var matches = mTagPatternRegex.Matches( inputText );
            // Perform regex matching
            foreach( Match match in matches )
            {
                var tagGroup = match.Groups["TAG"];
                if( tagGroup.Success )
                {
                    var tagToken = new Token( tagGroup.Value, TOKEN_TYPE_TAG, GetTagData( match ).ToArray(), tagGroup.Index );
                    if( OnTag != null )
                        OnTag( tagToken );
                    yield return tagToken;
                }
            }
        }

        private IEnumerable<TagInfo> GetTagData( Match tagMatch )
        {
            if( tagMatch == null )
                yield break;
            var linkGroup = tagMatch.Groups[ "LINK" ];
            if( linkGroup.Success )
            {
                foreach( Capture link in linkGroup.Captures )
                {
                    var tagInfo = new TagInfo();
                    tagInfo.Url = FindGroupCapture( tagMatch, "URL", link );
                    tagInfo.Caption = FindGroupCapture( tagMatch, "CAPTION", link );

                    yield return tagInfo;
                }
            }
        }

        private string FindGroupCapture( Match tagMatch, string innerGroupName, Capture outerGroupCapture )
        {
            if( tagMatch == null )
                return null;

            var innerGroup = tagMatch.Groups[innerGroupName];
            if( !innerGroup.Success )
                return null;

            foreach( Capture innerGroupCapture in innerGroup.Captures )
            {
                if( ( innerGroupCapture.Index >= outerGroupCapture.Index ) &&
                    ( innerGroupCapture.Index + innerGroupCapture.Length <= outerGroupCapture.Index + outerGroupCapture.Length ) )
                {
                    return innerGroupCapture.Value;
                }
            }

            return null;
        }
    }
}
