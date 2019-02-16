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

namespace CodeXCavator.Engine.StorageProviders
{
    /// <summary>
    /// FileSystemStorageProvider class.
    /// 
    /// The FileSystemStorageProvider class implements the IFileStorageProvider interface,
    /// and provides read only access to files located in the local file system.
    /// </summary>
    /// <seealso cref="IFileStorageProvider"/>
    public class FileSystemStorageProvider : IFileStorageProvider
    {

        public bool SupportsPath( string filePath )
        {
            return System.IO.Path.IsPathRooted( filePath ) || System.IO.File.Exists( filePath );
        }

        public System.IO.Stream GetFileStream( string filePath )
        {
            try
            {
                // Create a readonly stream from the specified file.
                System.IO.FileStream fileStream = new System.IO.FileStream( filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read );
                return fileStream;
            }
            catch
            {
                // Failed...
                return null;
            }
        }

        public long GetLastModificationTimeStamp( string filePath )
        {
            try
            {
                var fileInfo = new System.IO.FileInfo( filePath );
                if( fileInfo.Exists )
                    // Encode last write time as 64bit long.
                    return fileInfo.LastWriteTimeUtc.ToBinary();
            }
            catch
            {
            }
            // Failed...
            return 0;
        }

        public long GetSize( string filePath )
        {
            try
            {
                var fileInfo = new System.IO.FileInfo( filePath );
                if( fileInfo.Exists )
                    return fileInfo.Length;
            }
            catch
            {
            }
            // Failed...
            return 0;
        }

        public bool Exists( string filePath )
        {
            try
            {
                return System.IO.File.Exists( filePath );
            }
            catch
            {
            }
            return false;
        }
    }
}
