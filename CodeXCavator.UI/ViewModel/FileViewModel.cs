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

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// FileViewModel class.
    /// 
    /// This class implements the view model for a single file.
    /// </summary>
    internal class FileViewModel : ViewModelBase, IComparable<FileViewModel>, IComparable
    {
        /// <summary>
        /// Equality comparison.
        /// </summary>
        /// <param name="obj">Other, object which should be compared to the current one.</param>
        /// <returns>True, if the other object is equal to the current one, false otherwise.</returns>
        public override bool Equals( object obj )
        {
            var other = obj as FileViewModel;
            // Check, only if file paths do equal.
            if( other != null )
                return object.Equals( FilePath, other.FilePath );
            return false;
        }

        /// <summary>
        /// Hash code computation.
        /// </summary>
        /// <returns>Returns the hash code for the file view model.</returns>
        public override int GetHashCode()
        {
            return FilePath != null ? FilePath.GetHashCode() : 0;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="filePath">File path.</param>
        public FileViewModel( string filePath, object formattedFilePath = null )
        {
            FilePath = filePath;
            mFormattedFilePath = formattedFilePath;
        }

        /// <summary>
        /// Returns the file path.
        /// </summary>
        public string FilePath
        {
            get;
            private set;
        }

        protected object mFormattedFilePath;

        /// <summary>
        /// Returns a formatted file path.
        /// </summary>
        public object FormattedFilePath
        {
            get
            {
                if( mFormattedFilePath != null )
                    return mFormattedFilePath;
                return FilePath;
            }
        }

        /// <summary>
        /// Comparison. 
        /// </summary>
        /// <param name="other">Other file view model.</param>
        /// <returns>Returns a number, which defines, whether the other file path is greater, equal or less than the current one. Used for sorting.</returns>
        public int CompareTo( FileViewModel other )
        {
            if( other == null )
                return -1;
            return string.Compare( FilePath, other.FilePath );
        }

        /// <summary>
        /// Comparison. 
        /// </summary>
        /// <param name="obj">Other object.</param>
        /// <returns>Returns a number, which defines, whether the other file path is greater, equal or less than the current one. Used for sorting.</returns>
        public int CompareTo( object obj )
        {
            var other = obj as FileViewModel;
            return CompareTo( (FileViewModel) other );
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

        /// <summary>
        /// Returns a formatted file path with highlighted search hits.
        /// </summary>
        /// <param name="filePath">File path search hit.</param>
        /// <returns>Text run object.</returns>
        internal static TextBlock GetFormattedFilePath( SearchHitViewModel filePath )
        {
            var formattedPath = new TextBlock();

            string path = filePath.FilePath;
            int lastIndex = 0;
            foreach( var occurrence in filePath.SearchHit.Occurrences )
            {
                if( occurrence.Column > lastIndex )
                {
                    formattedPath.Inlines.Add( path.Substring( lastIndex, occurrence.Column - lastIndex ) );
                    lastIndex = occurrence.Column;
                }
                var inline = new Run( occurrence.Match );
                inline.Background = System.Windows.Media.Brushes.Aqua;
                formattedPath.Inlines.Add( inline );
                lastIndex = lastIndex + occurrence.Match.Length;
            }
            if( lastIndex < path.Length )
                formattedPath.Inlines.Add( path.Substring( lastIndex ) );
            return formattedPath;
        }
    }
}
