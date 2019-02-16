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
using System.IO;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// IndexViewModel class.
    /// 
    /// View model for a single index.
    /// </summary>
    internal class IndexViewModel : ViewModelBase
    {
        IIndex mIndex;

        /// <summary>
        /// Equality comparison.
        /// 
        /// Needed for ComboBox SelectedValue properly working.
        /// </summary>
        /// <param name="obj">Other index view model.</param>
        /// <returns>True, if the view models point to the same index, false otherwise.</returns>
        public override bool Equals( object obj )
        {
            IndexViewModel otherIndexViewModel = obj as IndexViewModel;
            if( otherIndexViewModel != null )
                return Object.Equals( mIndex, otherIndexViewModel.mIndex );
            return false;
        }

        /// <summary>
        /// Hashcode generation.
        /// 
        /// Needed for ComboBox SelectedValue properly working.
        /// </summary>
        /// <returns>Hashcode computed from the underlying index. I.e. if two different view models point to the same index, the hash code will be equal.</returns>
        public override int GetHashCode()
        {
            return mIndex != null ? mIndex.GetHashCode() : 0;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="index">Underlying index.</param>
        public IndexViewModel( IIndex index )        
        {
            mIndex = index;
        }

        /// <summary>
        /// Returns the underlying index.
        /// </summary>
        internal IIndex Index { get { return mIndex; } }

        /// <summary>
        /// Name of the index.
        /// </summary>
        public string Name
        {
            get { return System.IO.Path.GetFileName( mIndex.IndexPath ); }
        }

        /// <summary>
        /// Full file path of the index.
        /// </summary>
        public string Path
        {
            get { return mIndex.IndexPath;  }
        }

        /// <summary>
        /// Number of files in the index.
        /// </summary>
        public int FileCount
        {
            get
            {
                return mIndex.Files.Count();
            }
        }

        /// <summary>
        /// Enumeration of all files in the index.
        /// </summary>
        public IEnumerable<string> Files
        {
            get 
            {
                return mIndex.Files;
            }
        }

        /// <summary>
        /// Enumeration of tags contained in the index.
        /// </summary>
        public IEnumerable<TagInfo> Tags
        {
            get
            {
                return mIndex.Tags;
            }
        }

        /// <summary>
        /// Enumeration of all file types in the index.
        /// </summary>
        public IEnumerable<string> FileTypes
        {
            get
            {
                return mIndex.FileTypes;
            }
        }

        /// <summary>
        /// Time of last update of the index.
        /// </summary>
        public DateTime? LastUpdateTime 
        {
            get
            {
                try
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo( Path );
                    return directoryInfo.LastWriteTime;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
