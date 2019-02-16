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
using System.IO;
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// FileStorageProviders class.
    /// 
    /// The FileStorageProviders class is a global registry for file storage providers. 
    /// 
    /// It allows to register a file storage provider and allows to gain read only access
    /// to a file given it's path.
    /// 
    /// This registry can be used to add support for directly retrieving files from version control systems.
    /// </summary>
    public static class FileStorageProviders
    {
        private static List< IFileStorageProvider > sFileStorageProviders = new List< IFileStorageProvider >();

        static FileStorageProviders()
        {
            RegisterDefaultFileStorageProviders();
        }

        /// <summary>
        /// Registers all default file storage providers.
        /// </summary>
        private static void RegisterDefaultFileStorageProviders()
        {
            RegisterFileStorageProvider<StorageProviders.FileSystemStorageProvider>();
    
            PluginManager.LoadPlugins<Interfaces.IFileStorageProvider>();
        }

        /// <summary>
        /// Registers a file storage provider.
        /// </summary>
        /// <typeparam name="FILE_STORAGE_PROVIDER_TYPE">Type of the file storage provider, which should be registered.</typeparam>
        public static void RegisterFileStorageProvider< FILE_STORAGE_PROVIDER_TYPE >() where FILE_STORAGE_PROVIDER_TYPE : IFileStorageProvider, new()
        {
            RegisterFileEnumerator( new FILE_STORAGE_PROVIDER_TYPE() );
        }

        /// <summary>
        /// Registers a file storage provider.
        /// </summary>
        /// <param name="fileStorageProviderType">Type of the file storage provider, which should be registered.</param>
        public static void RegisterFileStorageProvider( Type fileStorageProviderType )
        {
            if( fileStorageProviderType != null && typeof( Interfaces.IFileStorageProvider ).IsAssignableFrom( fileStorageProviderType ) )
                RegisterFileEnumerator( Activator.CreateInstance( fileStorageProviderType ) as Interfaces.IFileStorageProvider );
        }

        /// <summary>
        /// Registers a file storage provider.
        /// </summary>
        /// <param name="storageProvider">File storage provider, which should be registered.</param>
        public static void RegisterFileEnumerator( IFileStorageProvider storageProvider )
        {
            if( storageProvider == null )
                return;
            sFileStorageProviders.Add( storageProvider );
        }

        /// <summary>
        /// Unregisters a file storage provider.
        /// </summary>
        /// <param name="storageProvider"></param>
        public static void UnregisterFileStorageProvider( IFileStorageProvider storageProvider )
        {
            sFileStorageProviders.Remove( storageProvider );
        }

        /// <summary>
        /// Retrieves a read only file stream for the specified path.
        /// 
        /// The registry iterates over all registered file storage providers and uses
        /// the first file storage provider capable of accessing the specified file for
        /// obtaining a read only stream.
        /// </summary>
        /// <param name="filePath">Path of the file, for which a readonly stream should be obtained.</param>
        /// <returns>Stream instance, or null, if no file storage provider could be found capable of handling the file.</returns>
        public static Stream GetFileStream( string filePath )
        {
            foreach( var storageProvider in sFileStorageProviders )
            {
                if( storageProvider.SupportsPath( filePath ) )
                {
                    var fileStream = storageProvider.GetFileStream( filePath );
                    if( fileStream != null )
                        return fileStream;
                }
            }
            return  null;
        }

        /// <summary>
        /// Returns the last modification time stamp of the specified file.
        /// </summary>
        /// <param name="filePath">Path of the file, for which a last modification time stamp should be obtained.</param>
        /// <returns>Timestamp of last modification, or 0 if no file storage provider could be found capable of handling the file.</returns>
        public static long GetLastModificationTimeStamp( string filePath )
        {
            foreach( var storageProvider in sFileStorageProviders )
            {
                if( storageProvider.SupportsPath( filePath ) )
                {
                    var lastModificationTimeStamp = storageProvider.GetLastModificationTimeStamp( filePath );
                    if( lastModificationTimeStamp != 0 )
                        return lastModificationTimeStamp;
                }
            }
            return 0;
        }

        /// <summary>
        /// Returns the size of the specified file in bytes.
        /// </summary>
        /// <param name="filePath">Path to the file, whose size should be determined.</param>
        /// <returns>Size of file in bytes.</returns>
        public static long GetSize( string filePath )
        {
            foreach( var storageProvider in sFileStorageProviders )
            {
                if( storageProvider.SupportsPath( filePath ) )
                {
                    var size = storageProvider.GetSize( filePath );
                    if( size != 0 )
                        return size;
                }
            }
            return 0;
        }

        /// <summary>
        /// Checks, whether the specified file exists.
        /// </summary>
        /// <param name="filePath">Path to the file, whose existence should be checked.</param>
        /// <returns>True, if the file exists, false otherwise.</returns>
        public static bool Exists( string filePath )
        {
            foreach( var storageProvider in sFileStorageProviders )
            {
                if( storageProvider.SupportsPath( filePath ) )
                {
                    if( storageProvider.Exists( filePath ) )
                        return true;
                }
            }
            return false;
        }
    }
}
