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

namespace CodeXCavator.Engine
{
    /// <summary>
    /// IndexFactory class.
    /// 
    /// Abstract factory for creating indexes, index builders and index searchers.
    /// </summary>
    public static class IndexFactory
    {
        /// <summary>
        /// Creates an in-memory index. 
        /// 
        /// Use the CreateBuilder() and CreateSearcher() methods of IIndex to construct and search the RAM based index.
        /// </summary>
        /// <returns>IIndex instance, which stores it's information in system memory thus providing 
        /// fastest indexing and search performance.</returns>
        public static IIndex CreateMemoryIndex()
        {
            return new LuceneIndex( new Lucene.Net.Store.RAMDirectory() );
        }

        /// <summary>
        /// Creates a file system based index.
        /// </summary>
        /// <param name="indexPath">Path to the root of the file based index.</param>
        /// <returns>IIndex instance, which allows to update or recreate the specified index.</returns>
        public static IIndex CreateFileSystemIndex( string indexPath )
        {
            return new LuceneIndex( indexPath );
        }

        /// <summary>
        /// Creates a file system based index builder.
        /// </summary>
        /// <param name="indexPath">Path to the root of the file based index.</param>
        /// <param name="overwrite">Specified, whether the index should be overwritten.</param>
        /// <returns>IIndex instance, which provides information about the specified index.</returns>
        public static IIndexBuilder CreateFileSystemIndexBuilder( string indexPath, bool overwrite = false )
        {
            return new LuceneIndexBuilder( indexPath, overwrite );
        }

        /// <summary>
        /// Creates a file system based index searcher.
        /// </summary>
        /// <param name="indexPath">Path to the root of the file based index.</param>
        /// <returns>IIndexSearcher instance, which searches the specified index.</returns>
        public static IIndexSearcher CreateFileSystemIndexSearcher( string indexPath )
        {
            return new LuceneIndexSearcher( indexPath );
        }
    }
}
