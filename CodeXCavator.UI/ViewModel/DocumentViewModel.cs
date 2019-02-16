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
using System.Windows.Documents;
using System.Collections.ObjectModel;
using CodeXCavator.Engine.Interfaces;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;
using CodeXCavator.Engine;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// DocumentViewModel class.
    /// 
    /// View model for displaying a text document.
    /// </summary>
    internal class DocumentViewModel : ViewModelBase
    {
        private static System.Windows.Media.SolidColorBrush HIGHLIGHT_BACKGROUND = new System.Windows.Media.SolidColorBrush( System.Windows.Media.Colors.Yellow );

        private string mFilePath;
        private string mText;
        private int mLineCount;

        private ReadOnlyCollection<OccurrenceViewModel> mOccurrences;
        private ReadOnlyCollection<IOccurrence> mOccurrenceModels;
        private ILookup<int, IOccurrence> mOccurrencesByLine;

        private IFileAction mFileActionCloseAllButThis;
        private IFileAction mFileActionCloseAll;
        
        private ContentControl mFileViewer;
        private DataTemplate mFileViewerTemplate;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="filePath">File path to the text document.</param>
        /// <param name="occurrences">Search result match occurrences.</param>
        /// <param name="searchQuery">Search query used for retrieving matches.</param>
        public DocumentViewModel( string filePath, ReadOnlyCollection<IOccurrence> occurrences = null, string searchQuery = null, IFileAction fileActionCloseAllButThis = null, IFileAction fileActionCloseAll = null )
        {
            mFilePath = filePath;
            mFileActionCloseAllButThis = fileActionCloseAllButThis;
            mFileActionCloseAll = fileActionCloseAll;
            mOccurrenceModels = occurrences;
            mOccurrencesByLine = occurrences != null ? occurrences.ToLookup( occurrence => occurrence.Line ) : null;
            if( occurrences != null )
                mOccurrences = new ReadOnlyCollection<OccurrenceViewModel>( occurrences.Select( occurrence => new OccurrenceViewModel( occurrence, null ) ).ToList() );
            SearchQuery = searchQuery;
        }

        /// <summary>
        /// Name of the text document.
        /// </summary>
        public string Name
        {
            get { return System.IO.Path.GetFileName( mFilePath ); }
        }

        /// <summary>
        /// Full file path of the text document.
        /// </summary>
        public string FilePath
        {
            get { return mFilePath; }
        }

        /// <summary>
        /// Returns all file actions available for the specified file.
        /// </summary>
        public IEnumerable<IFileAction> AvailableFileActions
        {
            get
            {
                bool separator = false;
                if( mFileActionCloseAll != null && mFileActionCloseAll.CanExecute( FilePath ) )
                {
                    yield return mFileActionCloseAll;
                    separator = true;
                }
                if( mFileActionCloseAllButThis != null && mFileActionCloseAllButThis.CanExecute( FilePath ) )
                {
                    yield return mFileActionCloseAllButThis;
                    separator = true;
                }
                if( separator )
                    yield return null;
                foreach( var fileAction in Engine.FileActions.GetExecutableActionsForFile( FilePath ) )
                    yield return fileAction;
            }
        }

        /// <summary>
        /// Text of the text document.
        /// </summary>
        public string Text
        {
            get
            {
                LoadText();
                return mText;
            }
        }

        /// <summary>
        /// Loads the text contents of the text document from the provided file path.
        /// </summary>
        private void LoadText()
        {
            if( mText == null )
            {
                try
                {
                    using( var inputStream = CodeXCavator.Engine.FileStorageProviders.GetFileStream( mFilePath ) )
                    {
                        using( var textReader = new System.IO.StreamReader( inputStream, Encoding.Default, true ) )
                        {
                            mText = textReader.ReadToEnd();
                        }
                    }
                }
                catch
                {
                    mText = "Error: Could not load file!";
                }

            }
        }

        /// <summary>
        /// Number of lines in current document
        /// </summary>
        public int LineCount
        {
            get
            {
                LoadText();
                mLineCount = TextUtilities.GetLineOffsets( mText ).Count() - 1;
                return mLineCount;
            }
        }

        /// <summary>
        /// Search query from which the match occurrences originated.
        /// </summary>
        public string SearchQuery
        {
            get;
            private set;
        }

        private OccurrenceViewModel mCurrentOccurrence;
        private int mCurrentOccurrenceIndex = -1;
				
        /// <summary>
        /// Currently selected and highlighted occurrence.
        /// </summary>
        public OccurrenceViewModel CurrentOccurrence
        {
            get { return mCurrentOccurrence; }
            set
            {
                if( mCurrentOccurrence != value )
                {
                    mCurrentOccurrence = value;
                    if( mOccurrences != null )
                        mCurrentOccurrenceIndex = mOccurrences.IndexOf( mCurrentOccurrence );
                    OnPropertyChanged( "CurrentOccurrence" );
                }
            }
        }

        /// <summary>
        /// Search result match occurrences.
        /// </summary>
        public ReadOnlyCollection<OccurrenceViewModel> Occurrences
        {
            get { return mOccurrences; }        
        }

        /// <summary>
        /// Search result match occurrence models.
        /// </summary>
        public ReadOnlyCollection<IOccurrence> OccurrenceModels
        {
            get { return mOccurrenceModels;  }
        }

        /// <summary>
        /// Search result match occurrence ranges.
        /// </summary>
        public IEnumerable<Tuple<int, int, int, int>> OccurrenceRanges
        {
            get
            {
                if( mOccurrenceModels != null )
                    return mOccurrenceModels.Select( occurrence => new Tuple<int, int, int, int>( occurrence.Column, occurrence.Line, occurrence.Column + occurrence.Match.Length, occurrence.Line ) );
                else
                    return null;
            }
        }

        /// <summary>
        /// Returns all match occurrences contained in the specified line.
        /// </summary>
        /// <param name="lineIndex"></param>
        /// <returns></returns>
        private IEnumerable<IOccurrence> GetOccurrencesInLine( int lineIndex )
        {
            if( mOccurrencesByLine != null )
                return mOccurrencesByLine[lineIndex];
            return null;
        }

        /// <summary>
        /// File viewer control.
        /// 
        /// This control displays the document content. It's used by the file viewer tab control,
        /// and solves problems with tab virtualization.
        /// </summary>
        public ContentControl FileViewer
        {
            get
            {
                if( mFileViewer == null )
                {
                    mFileViewer = new ContentControl();
                    mFileViewer.ContentTemplate = mFileViewerTemplate;
                    mFileViewer.Content = this;

                }
                return mFileViewer;
            }
        }

        /// <summary>
        /// Syntax highlighter to be used for the document.
        /// </summary>
        public IHighlighter Highlighter
        {
            get
            {
                return FileHighlighters.GetFileHighlighterForPath( mFilePath );
            }
        }

        /// <summary>
        /// File viewer template.
        /// 
        /// Data template for the viewer.
        /// </summary>
        public DataTemplate FileViewerTemplate
        {
            get
            {
                return mFileViewerTemplate;
            }
            set
            {
                if( mFileViewerTemplate != value )
                {
                    mFileViewerTemplate = value;
                    if( mFileViewer != null )
                        mFileViewer.ContentTemplate = mFileViewerTemplate;
                }
            }
        }

        /// <summary>
        /// Jumps to previous occurrence.
        /// </summary>
        internal void GotoPreviousOccurrence()
        {
            if( mOccurrences == null || mOccurrences.Count == 0 )
                return;
            if( mCurrentOccurrenceIndex >= 0 )
            {
                --mCurrentOccurrenceIndex;
                if( mCurrentOccurrenceIndex < 0 )
                    mCurrentOccurrenceIndex = mOccurrences.Count - 1;
            }
            else
            {
                mCurrentOccurrenceIndex = mOccurrences.Count - 1;
            }
            mCurrentOccurrence = mOccurrences[mCurrentOccurrenceIndex];
            OnPropertyChanged( "CurrentOccurrence" );
        }

        /// <summary>
        /// Jumps to next occurrence.
        /// </summary>
        internal void GotoNextOccurrence()
        {
            if( mOccurrences == null || mOccurrences.Count == 0 )
                return; 
            if( mCurrentOccurrenceIndex >= 0 )
            {
                ++mCurrentOccurrenceIndex;
                if( mCurrentOccurrenceIndex >= mOccurrences.Count )
                    mCurrentOccurrenceIndex = 0;
            }
            else
            {
                mCurrentOccurrenceIndex = 0;
            }
            mCurrentOccurrence = mOccurrences[mCurrentOccurrenceIndex];
            OnPropertyChanged( "CurrentOccurrence" );
        }
    }
}