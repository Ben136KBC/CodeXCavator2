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
using System.ComponentModel;
using CodeXCavator.Engine.Interfaces;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// SearchHitViewModel class.
    /// 
    /// View model for a single search hit.
    /// </summary>
    public class SearchHitViewModel : ViewModelBase
    {
        private static System.Runtime.CompilerServices.ConditionalWeakTable< ISearchHit, SearchHitViewModel > mViewModels = new System.Runtime.CompilerServices.ConditionalWeakTable<ISearchHit,SearchHitViewModel>();

        private Engine.Interfaces.ISearchHit mSearchHit;
        private IEnumerable<OccurrenceViewModel> mOccurrences;
        BackgroundWorker mOccurenceEvaluator; 
        private bool mIsExpanded;
        private ObservableEnumerable mParent;

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="searchHit">Underlying search hit.</param>
        private SearchHitViewModel( Engine.Interfaces.ISearchHit searchHit, ObservableEnumerable parent )
        {
            mParent = parent;
            mSearchHit = searchHit;
        }

        /// <summary>
        /// Returns an existing or creates a new view model for the specified search hit.
        /// </summary>
        /// <param name="searchHit">Search hit for which a view model should be returned, or created.</param>
        /// <param name="parent">Parent enumerable</param>
        /// <returns>Created or existing view model.</returns>
        public static SearchHitViewModel GetOrCreateSearchHitViewModel( Engine.Interfaces.ISearchHit searchHit, ObservableEnumerable parent )
        {
            SearchHitViewModel viewModel = null;
            if( !mViewModels.TryGetValue( searchHit, out viewModel ) )
            {
                viewModel = new SearchHitViewModel( searchHit, parent );
                mViewModels.Add( searchHit, viewModel );
            }
            return viewModel;
        }

        /// <summary>
        /// Performs finding match occurrences.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments.</param>
        private void occurenceEvaluator_DoWork( object sender, DoWorkEventArgs e )
        {
            // Just get the occurrences from the search hit, which does all the difficult work.
            e.Result = new Tuple<Engine.Interfaces.ISearchHit, ReadOnlyCollection<IOccurrence> >( (Engine.Interfaces.ISearchHit) e.Argument, ( (Engine.Interfaces.ISearchHit) e.Argument ).Occurrences );
        }

        /// <summary>
        /// Handles finishing of match occurrence retrival.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void occurenceEvaluator_RunWorkerCompleted( object sender, RunWorkerCompletedEventArgs e )
        {
            var searchHitAndOccurrences = (Tuple<Engine.Interfaces.ISearchHit, ReadOnlyCollection<IOccurrence> >) e.Result;
            var occurrences = searchHitAndOccurrences.Item2;
            if( occurrences != null )
            {
                // Create a list of occurrence view models from the gathered occurrences.
                Occurrences = occurrences.Select( occurrence => new OccurrenceViewModel( occurrence, searchHitAndOccurrences.Item1 ) ).ToArray();
                // Set the number of occurrences.
                NumberOfOccurrences = occurrences.Count;
            }
            else
            {
                Occurrences = new OccurrenceViewModel[] {};
                NumberOfOccurrences = 0;
            }
        }

        /// <summary>
        /// Returns the underlying search hit.
        /// </summary>
        public Engine.Interfaces.ISearchHit SearchHit
        {
            get
            {
                return mSearchHit;
            }
        }

        /// <summary>
        /// Returns the file path, which contains matches of the search query.
        /// </summary>
        public string FilePath
        {
            get
            {
                return mSearchHit.FilePath;
            }
        }

        int? mNumberOfOccurrences;

        /// <summary>
        /// Returns the number of match occurrences within the file.
        /// 
        /// This is intentionally Nullable. If this value is null, the UI displays an in-progress indicator
        /// in order to signalize that finding matches inside the file is in progress.
        /// </summary>
        public int? NumberOfOccurrences
        {
            get
            {
                return mNumberOfOccurrences;
            }
            set
            {
                if( mNumberOfOccurrences != value )
                {
                    mNumberOfOccurrences = value;
                    OnPropertyChanged( "NumberOfOccurrences" );
                }
            }
        }

        /// <summary>
        /// Returns an enumeration of occurrence view models.
        /// 
        /// The getter triggers a background worker in order to retrieve the occurrences within the file.
        /// </summary>
        public IEnumerable<OccurrenceViewModel> Occurrences
        {
            get
            {
                // If not searching yet inside the file...
                if( mOccurrences == null && mOccurenceEvaluator == null )
                {
                    // ...trigger a background worker to find the matches inside the file.
                    mOccurenceEvaluator = new BackgroundWorker();
                    mOccurenceEvaluator.DoWork += new DoWorkEventHandler( occurenceEvaluator_DoWork );
                    mOccurenceEvaluator.RunWorkerCompleted += new RunWorkerCompletedEventHandler( occurenceEvaluator_RunWorkerCompleted );
                    mOccurenceEvaluator.RunWorkerAsync( mSearchHit );
                }
                return mOccurrences;
            }

            internal set
            {
                if( mOccurrences != value )
                {
                    mOccurrences = value;
                    mOccurenceEvaluator = null;
                    OnPropertyChanged( "Occurrences" );
                }
            }
        }

        private static Brush mBackground = new SolidColorBrush( Color.FromArgb( 0x40, 0xff, 0x80, 0x00 ) );

        /// <summary>
        /// Background color, which should be used for coloring search hit entries.
        /// </summary>
        public Brush Background
        {
            get { return mBackground; }
        }

        /// <summary>
        /// Determines, whether the search hit entry is expanded or not, i.e. whehter it's child 
 				/// occurrences should be listed.
        /// </summary>
        public bool IsExpanded
        {
            get
            {
                return mIsExpanded;
            }
            set
            {
                if( mIsExpanded != value )
                {
                    mIsExpanded = value;
                    OnPropertyChanged("IsExpanded");
                    if( mParent != null )
                        mParent.Update();
                }
            }
        }

        /// <summary>
        /// Returns all file actions available for the specified file.
        /// </summary>
        public IEnumerable<IFileAction> AvailableFileActions
        {
            get
            {
                return Engine.FileActions.GetExecutableActionsForFile( FilePath );
            }
        }
    }
}
