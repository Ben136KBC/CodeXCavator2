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

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// FileTypeFilter class.
    /// 
    /// View model for the file type filter.
    /// </summary>
    internal class FileTypeFilter : ViewModelBase
    {
        HashSet<string> mAllowedFileTypes = new HashSet<string>( StringComparer.OrdinalIgnoreCase );

        public FileTypeFilter( bool enabled = true )
        {
            mIsEnabled = true;
        }

        public FileTypeFilter( bool enabled = true, params string[] fileTypes )
        {
            mIsEnabled = true;
            mAllowedFileTypes = new HashSet<string>( fileTypes );
        }

        /// <summary>
        /// List of allowed file types.
        /// </summary>
        public string[] AllowedFileTypes
        {
            get
            {
                if( !mIsEnabled )
                    return null;
                return mAllowedFileTypes.ToArray();
            }
        }

        /// <summary>
        /// Resets the file type filter.
        /// </summary>
        public void Reset()
        {
            mAllowedFileTypes.Clear();
        }

        /// <summary>
        /// Notifies of change of the file type filter.
        /// </summary>
        public void Update()
        {
            OnPropertyChanged( "AllowedFileTypes" );
        }

        /// <summary>
        /// Adds a file type to the filter.
        /// </summary>
        /// <param name="extension">Extension of the file type, to be added to the filter.</param>
        public void AddFileType( string extension )
        {
            int setSize = mAllowedFileTypes.Count;
            mAllowedFileTypes.Add( extension );
            if( mAllowedFileTypes.Count != setSize )
                OnPropertyChanged( "AllowedFileTypes" );
        }

        /// <summary>
        /// Removes a file type from the filter.
        /// </summary>
        /// <param name="extension">Extension of the file type, to be removed from the filter.</param>
        public void RemoveFileType( string extension )
        {
            int setSize = mAllowedFileTypes.Count;
            mAllowedFileTypes.Remove( extension );
            if( mAllowedFileTypes.Count != setSize )
                OnPropertyChanged( "AllowedFileTypes" );
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
    /// FileTypeFilterItemViewModel class.
    /// 
    /// View model for a single file type file filter entry.
    /// </summary>
    internal class FileTypeFilterItemViewModel : ViewModelBase, IComparable
    {
        protected string mFileType;
        protected bool mEnabled;
        FileTypeFilter mParentFilter;

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="parentFilter">Parent file filter, the entry belongs to.</param>
        /// <param name="fileType">File type, the filter entry belongs to.</param>
        public FileTypeFilterItemViewModel( FileTypeFilter parentFilter, string fileType )
        {
            mFileType = fileType;
            mEnabled = true;
            mParentFilter = parentFilter;
            if( mEnabled )
                mParentFilter.AddFileType( mFileType );
            else
                mParentFilter.RemoveFileType( mFileType );
        }

        /// <summary>
        /// File type, the filter entry belongs to.
        /// </summary>
        public string FileType
        {
            get { return mFileType; }
        }

        /// <summary>
        /// Determines, whether the file type shold be included in the search result list, or not.
        /// </summary>
        public bool Enabled
        {
            get { return mEnabled;  }
            set
            {
                if( mEnabled != value )
                {
                    // Notify parent file type filter of change. This will retrigger search.
                    if( mParentFilter != null )
                    {
                        if( value )
                            mParentFilter.AddFileType( mFileType );
                        else
                            mParentFilter.RemoveFileType( mFileType );
                    }                        
                    mEnabled = value;
                    OnPropertyChanged( "Enabled" );
                }
            }
        }
       
        /// <summary>
        /// Compares the entry with another. This is needed for sorting.
        /// </summary>
        /// <param name="obj">Other entry.</param>
        /// <returns>Comparison result. 0 if equal, positive if obj GREATER this, negative if obj LESS this.</returns>
        public int CompareTo( object obj )
        {
            return mFileType.CompareTo( ( (FileTypeFilterItemViewModel) obj ).mFileType );
        }
    }
}
