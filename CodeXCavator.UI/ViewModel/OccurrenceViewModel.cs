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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// OccurrenceViewModel class.
    /// 
    /// View model for a single match occurrence.
    /// </summary>
    public class OccurrenceViewModel : ViewModelBase
    {
        private IOccurrence mOccurrence;
        private ISearchHit mParentSearchHit;

        public override bool Equals( object obj )
        {
            var occurrence = obj as OccurrenceViewModel;
            if( occurrence != null )
                return object.Equals( mOccurrence, occurrence.mOccurrence );
            return false;
        }

        public override int GetHashCode()
        {
            return mOccurrence != null ? mOccurrence.GetHashCode() : 0;
        }

        /// <summary>
        /// Underlying occurrence.
        /// </summary>
        public IOccurrence Occurrence
        {
            get
            {
                return mOccurrence;
            }
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="occurrence">Underlying occurrence.</param>
        public OccurrenceViewModel( IOccurrence occurrence, ISearchHit parentSearchHit )
        {
            mOccurrence = occurrence;
            mParentSearchHit = parentSearchHit;
        }

        /// <summary>
        /// Parent search hit, to which the occurrence belongs.
        /// </summary>
        public ISearchHit SearchHit
        {
            get { return mParentSearchHit; }
        }

        /// <summary>
        /// File path of the file containing the occurrence
        /// </summary>
        public string FilePath
        {
            get { return mParentSearchHit != null ? mParentSearchHit.FilePath : null; }
        }

        /// <summary>
        /// Text matching the a search query.
        /// </summary>
        public string Match
        {
            get
            {
                return mOccurrence.Match;
            }
        }

        /// <summary>
        /// Line number of the mach (1-based!)
        /// </summary>
        public int Line
        {
            get
            {
                return mOccurrence.Line + 1;
            }
        }

        /// <summary>
        /// Column number of the match (1-based!)
        /// </summary>
        public int Column
        {
            get
            {
                return mOccurrence.Column + 1;
            }
        }

        /// <summary>
        /// Position of the match ( Line, Column )
        /// </summary>
        public string Position
        {
            get
            {
                return string.Format( "({0},{1})", Line, Column );
            }
        }

        /// <summary>
        /// Fragment text containing the match.
        /// </summary>
        public string Fragment
        {
            get
            {
                StringBuilder fragmentBuilder = new StringBuilder();
                foreach( var line in mOccurrence.Fragment )
                {
                    fragmentBuilder.Append( line.Key );
                }
                return fragmentBuilder.ToString();
            }
        }

        private static System.Windows.Media.SolidColorBrush HIGHLIGHT_BACKGROUND = new System.Windows.Media.SolidColorBrush( System.Windows.Media.Colors.Yellow );

        /// <summary>
        /// Formatted fragment.
        /// 
        /// A textblock where the match is highlighted by a different background color.
        /// </summary>
        public TextBlock FormattedFragment
        {
            get
            {
                var tb = new TextBlock();
                foreach( var line in mOccurrence.Fragment )
                {
                    if( line.Value == mOccurrence.Line )
                    {
                        string beforeHighlight = line.Key.Substring( 0, mOccurrence.Column );
                        string afterHighlight = line.Key.Substring( mOccurrence.Column + Match.Length );
                        // Add three runs.
                        tb.Inlines.Add( new Run( beforeHighlight ) );
                        // This run contains the match, and thus has a different background.
                        tb.Inlines.Add( new Run( Match ) { Background = HIGHLIGHT_BACKGROUND } );
                        tb.Inlines.Add( new Run( afterHighlight ) );
                    }
                    else                    
                    {
                        tb.Inlines.Add( new Run( line.Key ) );
                    }
                }
                return tb;
            }
        }

        private static Brush mBackground = new SolidColorBrush( Color.FromArgb( 0x40, 0x80, 0xff, 0x00 ) );

        /// <summary>
        /// Background color, which should be used for coloring occurrence entries.
        /// </summary>
        public Brush Background
        {
            get { return mBackground; }
        }

    }
}
