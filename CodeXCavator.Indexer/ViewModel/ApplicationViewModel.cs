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
using System.Windows;

namespace CodeXCavator.Indexer.ViewModel
{
    /// <summary>
    /// ApplicationViewModel class.
    /// 
    /// This is the view model for the whole application.
    /// </summary>
    internal class ApplicationViewModel : ViewModelBase
    {
        App mApp;

        /// <summary>
        /// Constructor.
        /// </summary>
        internal ApplicationViewModel()
        {
            mApp = Application.Current as App;
        }

        /// <summary>
        /// Number of index builders, which are currently active.
        /// </summary>
        internal int IndexBuildersActive;
        
        private int mIndexBuildersFinished;

        /// <summary>
        /// Number of index builders, which have finished indexing.
        /// </summary>
        internal int IndexBuildersFinished
        {
            get { return mIndexBuildersFinished; }
            set 
            {
                if( mIndexBuildersFinished != value )
                {
                    mIndexBuildersFinished = value;
                    OnPropertyChanged( "IndexBuildersFinished" );
                    if( IsFinished )
                        OnPropertyChanged( "IsFinished" );
                }
            }
        }

        /// <summary>
        /// Indicates, whether the application is currently indexing.
        /// </summary>
        public bool IsIndexing
        {
            get
            {
                return IndexBuildersActive > 0;
            }
        }

        /// <summary>
        /// Checks, whether the indexer is finished.
        /// </summary>
        public bool IsFinished
        {
            get
            {
                return IndexBuildersFinished == mApp.IndexBuilders.Count();
            }
        }

        private IList<IndexBuilderViewModel> mIndexBuilders;

        /// <summary>
        /// Enumeration of index builder view models.
        /// </summary>
        public IEnumerable<IndexBuilderViewModel> IndexBuilders
        {
            get
            {
                if( mApp != null && mIndexBuilders == null )
                {
                    mIndexBuilders = new List<IndexBuilderViewModel>();

                    int builderCount = 0;
                    foreach( var indexBuilder in mApp.IndexBuilders )
                    {
                        bool launchImmediately = !mApp.Multithreading || ( builderCount < mApp.MaxIndexingWorkers );
                        var indexBuilderViewModel = new IndexBuilderViewModel( this, indexBuilder, launchImmediately );
                        indexBuilderViewModel.PropertyChanged += HandleIndexBuilderPropertyChange;
                        mIndexBuilders.Add( indexBuilderViewModel );
                        yield return indexBuilderViewModel;
                        ++builderCount;
                    }
                }
            }
        }

        private void HandleIndexBuilderPropertyChange( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( mIndexBuilders == null )
                return;

            if( e.PropertyName == "IsFinished" )
            {
                LaunchNextUnfinishedIndexBuilder();
            }
        }

        private void LaunchNextUnfinishedIndexBuilder()
        {
            foreach( var indexBuilder in mIndexBuilders )
            {
                if( !indexBuilder.IsFinished && !indexBuilder.IsIndexing )
                {
                    indexBuilder.Launch();
                    break;
                }
            }
        }
    }
}
