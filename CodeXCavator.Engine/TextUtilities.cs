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

namespace CodeXCavator.Engine
{
    /// <summary>
    /// Text position structure.
    /// </summary>
    public struct TextPosition
    {
        public TextPosition( int column, int line )
        {
            Column = column;
            Line = line;
        }

        public static implicit operator TextPosition( Tuple<int, int> tuple )
        {
            return new TextPosition( tuple.Item1, tuple.Item2 );
        }

        public int Column;
        public int Line;

        public static readonly TextPosition Invalid = new TextPosition( -1, -1 );

        public static bool operator == ( TextPosition a, TextPosition b )
        {
            return a.Column == b.Column && a.Line == b.Line;
        }

        public static bool operator !=( TextPosition a, TextPosition b )
        {
            return a.Column != b.Column || a.Line != b.Line;
        }

        public override bool Equals( object obj )
        {
            return base.Equals( obj );
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <summary>
    /// Stores information about line breaks.
    /// </summary>
    public struct LineInfo
    {
        public LineInfo( int numberOfLines, int lastLineOffset )
        {
            NumberOfLines = numberOfLines;
            LastLineOffset = lastLineOffset;
        }

        /// <summary>
        /// Number of lines
        /// </summary>
        public int NumberOfLines;
        /// <summary>
        /// Offset of last line
        /// </summary>
        public int LastLineOffset;
    }

    public static class TextUtilities
    {
        // Default word boundary separator chars.
        public static readonly char[] DEFAULT_SEPARATORS = { '.', ',', ':', '[', ']', '+', '-', '*', '/', '\\', '%', '?', '(', ')', '<', '>', ';', '|', '!', '&', '^', '\'', '\"', '=', '~' };

        /// <summary>
        /// Internal line scanner state.
        /// </summary>
        private enum LineScannerState
        {
            Init,
            LineBreak,
        }

        /// <summary>
        /// Enumerates the offsets of line breaks within the provided text.
        /// 
        /// This handles UNIX, MAC, WINDOWS and mixed line breaks properly.
        /// </summary>
        /// <param name="text">Text for which line break offsets should be returned.</param>
        /// <returns>Enumeration of line break character offsets.</returns>
        public static IEnumerable<int> GetLineOffsets( string text )
        {
            yield return 0;
            foreach( int lineOffset in GetLineOffsets( text, 0, text.Length ) )
                yield return lineOffset;
            // Last virtual line... This is needed for the binary search algorithm to work properly.
            yield return text.Length;
        }

        /// <summary>
        /// Enumerates the offsets of line breaks within the provided text.
        /// 
        /// This handles UNIX, MAC, WINDOWS and mixed line breaks properly.
        /// </summary>
        /// <param name="text">Text for which line break offsets should be returned.</param>
        /// <param name="startPosition">Position at which the search should start.</param>
        /// <param name="endPosition">Position at which the search should end.</param>
        /// <returns>Enumeration of line break character offsets.</returns>
        public static IEnumerable<int> GetLineOffsets( string text, int startPosition, int endPosition )
        {
            LineScannerState scannerState = LineScannerState.Init;

            for( int currentPos = startPosition ; currentPos < endPosition ; ++currentPos )
            {
                Char currentChar = text[currentPos];
                switch( scannerState )
                {
                    case LineScannerState.Init:
                        {
                            if( currentChar == '\r' )
                                scannerState = LineScannerState.LineBreak;
                            else
                                if( currentChar == '\n' )
                                {
                                    yield return currentPos + 1;
                                }
                        }
                        break;
                    case LineScannerState.LineBreak:
                        {
                            if( currentChar == '\n' )
                            {
                                yield return currentPos + 1;
                                scannerState = LineScannerState.Init;
                            }
                            else
                                if( currentChar == '\r' )
                                {
                                    yield return currentPos;
                                }
                                else
                                {
                                    yield return currentPos;
                                    scannerState = LineScannerState.Init;
                                }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Converts a column and line index to a text offset.
        /// </summary>
        /// <param name="columnAndLineIndex">Text position consisting of column and line index ( zero based ).</param>
        /// <param name="lineOffsets">Array of line offsets.</param>
        /// <returns>Text offset corresponding to the specified text position, or -1 if the text position is invalid.</returns>
        public static int ColumnAndLineIndexToOffset( TextPosition columnAndLineIndex, int[] lineOffsets )
        {
            if( columnAndLineIndex == TextPosition.Invalid || lineOffsets == null )
                return -1;
            if( columnAndLineIndex.Line < lineOffsets.Length )
                return lineOffsets[columnAndLineIndex.Line] + columnAndLineIndex.Column;
            return -1;
        }

        /// <summary>
        /// Converts a text offset to a column and line index.
        /// </summary>
        /// <param name="textOffset">Text offset, which should be converted.</param>
        /// <param name="lineOffsets">Array of line offsets.</param>
        /// <returns>Tuple containing the column and line index ( zero based ) corresponding to the specified text offset</returns>
        public static TextPosition OffsetToColumnAndLineIndex( int textOffset, int[] lineOffsets )
        {
            if( lineOffsets != null )
            {
                int line = Array.BinarySearch<int>( lineOffsets, textOffset );
                if( line < 0 )
                    line = -line - 2;
                if( line < lineOffsets.Length )
                {
                    return new TextPosition( textOffset - lineOffsets[line], line );
                }
            }
            return TextPosition.Invalid;
        }

         /// <summary>
        /// Converts a text offset to a line index.
        /// </summary>
        /// <param name="textOffset">Text offset, which should be converted.</param>
        /// <param name="lineOffsets">Array of line offsets.</param>
        /// <returns>Line index ( zero base ) corresponding to the specified text offset, or -1 if the text offset is outside the range.</returns>
        public static int OffsetToLineIndex( int textOffset, int[] lineOffsets )
        {
            return OffsetToColumnAndLineIndex( textOffset, lineOffsets ).Line;
        }

        /// <summary>
        /// Searches the start of a word beginning search at the specified position.
        /// </summary>
        /// <param name="text">Text, within which a word start should be searched.</param>
        /// <param name="textOffset">Start position of search.</param>
        /// <returns>Text offset to the start of the found word.</returns>
        public static int FindWordStartAtOffset( string text, int textOffset )
        {
            if( text == null )
                return -1;

            if( textOffset > text.Length )
                textOffset = text.Length - 1;
            if( textOffset < 0 )
                textOffset = 0;

            if( text[textOffset] == '\n' || text[textOffset] == '\r' )
                return -1;

            if( DEFAULT_SEPARATORS.Contains( text[textOffset] ) )
                return textOffset;

            if( Char.IsWhiteSpace( text[textOffset] ) )
            {
                while( textOffset > 0 && Char.IsWhiteSpace( text[textOffset] ) )
                    --textOffset;
            }
            else
            {
                while( textOffset >= 0 && !Char.IsWhiteSpace( text[textOffset] ) && !DEFAULT_SEPARATORS.Contains( text[textOffset] ) )
                    --textOffset;
            }
            
            return textOffset + 1;
        }

        /// <summary>
        /// Searches the end of a word beginning search at the specified position.
        /// </summary>
        /// <param name="text">Text, within which a word start should be searched.</param>
        /// <param name="textOffset">Start position of search.</param>
        /// <returns>Text offset to the end of the found word.</returns>
        public static int FindWordEndAtOffset( string text, int textOffset )
        {
            if( text == null )
                return -1;

            if( textOffset > text.Length )
                textOffset = text.Length - 1;
            if( textOffset < 0 )
                textOffset = 0;

            if( text[textOffset] == '\n' || text[textOffset] == '\r' )
                return -1;

            if( DEFAULT_SEPARATORS.Contains( text[textOffset] ) )
                return textOffset + 1;

            if( Char.IsWhiteSpace( text[textOffset] ) )
            {
                while( textOffset > 0 && Char.IsWhiteSpace( text[textOffset] ) )
                    ++textOffset;
            }

            while( textOffset > 0 && !Char.IsWhiteSpace( text[textOffset] ) && !DEFAULT_SEPARATORS.Contains( text[textOffset] ) )
                ++textOffset;
            
            return textOffset;        
        }

        /// <summary>
        /// Counts the number of lines in the text between the specified range and returns the number of lines
        /// and the offset of the last encountered line.
        /// </summary>
        /// <param name="text">Text which should be searched for line breaks.</param>
        /// <param name="startPos">Start position of the range, which should be searched.</param>
        /// <param name="endPos">End position of the range, which should be searched.</param>
        /// <returns>Line info structure containing the number of counted line breaks and the offset of the last line.</returns>
        public static LineInfo CountLinesInRangeAndGetOffsetOfLastFoundLine( string text, int startPos, int endPos )
        {
            int numberOfLines = 0;
            int lastLineOffset = -1;
            foreach( var lineOffset in TextUtilities.GetLineOffsets( text, startPos, endPos ) )
            {
                ++numberOfLines;
                lastLineOffset = lineOffset;
            }
            return new LineInfo( numberOfLines, lastLineOffset );
        }

        /// <summary>
        /// Checks, whether the specified character is a separator character.
        /// </summary>
        /// <param name="c">Character.</param>
        /// <returns>True, if the character is a separator character, false otherwise.</returns>
        public static bool IsSeparator( char c )
        {
            return DEFAULT_SEPARATORS.Contains( c );
        }

        /// <summary>
        /// Checks, whether a match, is a whole word.
        /// </summary>
        /// <param name="text">Text, which contains a match.</param>
        /// <param name="matchPos">Position of the match within the text.</param>
        /// <param name="matchLength">Length of the match.</param>
        /// <returns>True, if the match is a whole word (i.e. whether it starts and ends on a word boundary ), false otherwise.</returns>
        internal static bool IsMatchWholeWord( string text, int matchPos, int matchLength )
        {
            bool leftIsSeparator = false;
            if( matchPos == 0 )
                leftIsSeparator = true;
            else
                if( IsSeparator( text[matchPos - 1] ) || char.IsWhiteSpace( text[matchPos - 1] ) )
                    leftIsSeparator = true;

            bool rightIsSeparator = false;
            if( matchPos + matchLength >= text.Length )
                rightIsSeparator = true;
            else
                if( IsSeparator( text[matchPos + matchLength] ) || char.IsWhiteSpace( text[matchPos + matchLength ] ) )
                    rightIsSeparator = true;

            return ( leftIsSeparator && rightIsSeparator );
        }
    }
}
