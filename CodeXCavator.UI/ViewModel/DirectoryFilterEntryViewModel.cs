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
using System.Collections.ObjectModel;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// DirectoryFilter class.
    /// 
    /// View model class for the directory filter.
    /// </summary>
    internal class DirectoryFilter : ViewModelBase
    {
        ObservableCollection<DirectoryFilterEntryViewModel> mDirectories;

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="directories">Directory filter entry viewmodels.</param>
        public DirectoryFilter( ObservableCollection<DirectoryFilterEntryViewModel> directories, bool enabled = true )
        {
            mIsEnabled = enabled;
            mDirectories = directories;
            mDirectories.CollectionChanged += new System.Collections.Specialized.NotifyCollectionChangedEventHandler( DirectoriesCollectionChanged );
        }

        /// <summary>
        /// Handles the change of the directory filter entry collection.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        void DirectoriesCollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
        {
            // Detach old items from the directory filter
            if( e.OldItems != null )
                foreach( DirectoryFilterEntryViewModel directory in e.OldItems )
                    if( directory != null )
                        directory.DirectoryFilter = null;
            // Attach new items to the directory filter.
            if( e.NewItems != null )
                foreach( DirectoryFilterEntryViewModel directory in e.NewItems )
                    if( directory != null )
                        directory.DirectoryFilter = this;
            // Update the directory filter.
            Update();
        }

        /// <summary>
        /// List of allowed directories.
        /// </summary>
        public Tuple<string,bool,bool>[] AllowedDirectories
        {
            get
            {
                if( !mIsEnabled )
                    return null;
                return mDirectories.Select( directory => new Tuple<string,bool,bool>( directory.Pattern, directory.Recursive, directory.Exclusive ) ).ToArray();
            }
        }

        /// <summary>
        /// Resets the directory filter.
        /// </summary>
        public void Reset()
        {
            mDirectories.Clear();
        }

        /// <summary>
        /// Notifies of directory filter change.
        /// </summary>
        internal void Update()
        {
            OnPropertyChanged( "AllowedDirectories" );
        }

        private bool mIsEnabled;

        /// <summary>
        /// Determines, whether the filter is enabled or not.
        /// </summary>
        public bool IsEnabled
        {
            get { return mIsEnabled; }
            set
            {
                if( mIsEnabled != value )
                {
                    mIsEnabled = value;
                    OnPropertyChanged( "Enabled" );
                    Update();
                }
            }
        }
    }

    /// <summary>
    /// DirectoryFilterEntryViewModel class.
    /// 
    /// View model class for a single directory filter entry.
    /// </summary>
    public class DirectoryFilterEntryViewModel : ViewModelBase
    {
        private string mPattern;
        private bool mRecursive;    
        private bool mExclusive;

        /// <summary>
        /// Directory filter, the filter entry belongs to.
        /// </summary>
        internal DirectoryFilter DirectoryFilter { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public DirectoryFilterEntryViewModel()
        {
            mPattern = string.Empty;
            mRecursive = true;
            mExclusive = false;
        }

        /// <summary>
        /// Directory wildcard pattern.
        /// </summary>
        public string Pattern            
        {
            get { return mPattern; }
            set 
            { 
                if( mPattern != value )
                {
                    mPattern = value;
                    OnPropertyChanged( "Pattern" );
                    // Notify parent directory filter of change. This will retrigger search.
                    if( DirectoryFilter != null )
                        DirectoryFilter.Update();
                }
            }
        }

        /// <summary>
        /// Recursive flag.
        /// </summary>
        public bool Recursive
        {
            get { return mRecursive; }
            set
            {
                if( mRecursive != value )
                {
                    mRecursive = value;
                    OnPropertyChanged( "Recursive" );
                    // Notify parent directory filter of change. This will retrigger search.
                    if( DirectoryFilter != null )
                        DirectoryFilter.Update();
                }
            }
        }

        /// <summary>
        /// Exclusive flag.
        /// </summary>
        public bool Exclusive
        {
            get { return mExclusive; }
            set
            {
                if( mExclusive != value )
                {
                    mExclusive = value;
                    OnPropertyChanged( "Exclusive" );
                    // Notify parent directory filter of change. This will retrigger search.
                    if( DirectoryFilter != null )
                        DirectoryFilter.Update();
                }
            }
        }

    }
}
