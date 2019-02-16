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

namespace CodeXCavator.Engine.Filters
{
    /// <summary>
    /// FileCatalogueEnumeratorFileFilter class.
    /// 
    /// The FileCatalogueEnumerator file filter is responsible for applying a file catalogue enumerator on 
    /// any file in the input set. It determines the file catalogue enumerator, which can be used with 
    /// the input file, and adds the files contained in the catalogue to the input set.
    /// </summary>
    public class FileCatalogueEnumeratorFileFilter : IFileFilter
    {
        /// <summary>
        /// List of file catalogue enumerators, to be used by the file catalogue enumerator file filter.
        /// 
        /// The Key identifies the file extension of the file catalogue, which should be enumerated by the
        /// enumerator specified by the Value field of the key value pair. The Value tuple consists 
        /// of the file catalogue enumerator, a flag indicating, whether the file catalogue should be enumerated
        /// recursively, and a flag indicating whether the file catalogue file itself should be filtered out or not.
        /// </summary>
        public IEnumerable<KeyValuePair<string, Tuple< IFileCatalogueEnumerator, bool, bool>>> FileEnumerators { get; set; }
        /// <summary>
        /// Determines, whether the file catalogue file should be filtered out by the filter or not.
        /// </summary>
        bool FilterOutFileCatalogue { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FileCatalogueEnumeratorFileFilter()
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        public FileCatalogueEnumeratorFileFilter( bool filterOutFileCatalogue )
        {
            FilterOutFileCatalogue = filterOutFileCatalogue;
        }
        
        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="fileEnumerators">List of file catalogue enumerators, to be used by the file catalogue enumerator file filter.</param>
        public FileCatalogueEnumeratorFileFilter( params KeyValuePair<string, Tuple<IFileCatalogueEnumerator, bool, bool>>[] fileEnumerators )
        {
            FileEnumerators = fileEnumerators;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="fileEnumerators">List of file catalogue enumerators, to be used by the file catalogue enumerator file filter.</param>
        public FileCatalogueEnumeratorFileFilter( IEnumerable<KeyValuePair<string, Tuple<IFileCatalogueEnumerator, bool, bool>>> fileEnumerators )
        {
            FileEnumerators = fileEnumerators;
        }

        /// <summary>
        /// Filters the list of files.
        /// 
        /// The file catalogue filter, acts as an emitting filter. Every time it finds a file, which
        /// matches one of the specified file catalogue enumerators, it uses the file catalogue enumerator
        /// to enumerates it's contents. Otherwise it simple passes the file thru.
        /// 
        /// Whether the file catalogue file itself is filtered out, or also passed thru, is either determined 
        /// by the FilterOutFileCatalogue property, or by the third element of the tuple in the FileEnumerators list.
        /// 
        /// This filter can be used, together with a directory enumerator, which enumerates project files. If the
        /// file catalogue enumerator filter is initialized with a file catalogue enumerator, which is capable to 
        /// read and enumerate the contents of a project file enumerated by the directory enumerator, the resulting
        /// list would be a list of all files contained in all projectes enumerated by the directory enumerator.
        /// </summary>
        /// <param name="files">List of files, which should be filtered.</param>
        /// <returns>Fitered file list.</returns>
        public IEnumerable<string> Filter( IEnumerable<string> files )
        {
            Dictionary< string, Tuple< IFileCatalogueEnumerator, bool, bool > > fileEnumerators = FileEnumerators != null ? FileEnumerators.ToDictionary( fileEnumeratorEntry => fileEnumeratorEntry.Key, fileEnumeratorEntry => fileEnumeratorEntry.Value, StringComparer.OrdinalIgnoreCase ) : null;
            foreach( var filePath in files )
            {
                string fileExtension = System.IO.Path.GetExtension( filePath );
                if( fileEnumerators == null )
                {
                    var registeredEnumerator = Engine.FileCatalogueEnumerators.GetFileCatalogueEnumerator( fileExtension );
                    if( registeredEnumerator != null )
                    {
                        foreach( var enumeratedFile in registeredEnumerator.EnumerateFiles( filePath, true ) )
                            yield return enumeratedFile;
                    }
                    else
                    {
                        // Try to get a file catalogue filter from the file catalogue filter registry.
                        registeredEnumerator = Engine.FileCatalogueEnumerators.GetFileCatalogueEnumeratorForPath( filePath );
                        if( registeredEnumerator != null )
                        {
                            // Enumerate file catalogue file, if requested.
                            if( !FilterOutFileCatalogue )
                                yield return filePath;
                            // Enumerate files contained in file catalogue
                            foreach( var enumeratedFile in registeredEnumerator.EnumerateFiles( filePath, true ) )
                                yield return enumeratedFile;
                        }
                        else
                        {
                            yield return filePath;
                        }
                    }
                }
                else
                {
                    Tuple< IFileCatalogueEnumerator, bool, bool > matchingFileEnumeratorEntry;
                    // Try to get a file catalogue filter by extension.
                    if( fileEnumerators.TryGetValue( fileExtension, out matchingFileEnumeratorEntry ) )
                    {
                        // Enumerate file catalogue file, if requested.
                        if( !matchingFileEnumeratorEntry.Item3 )
                            yield return filePath;  
                        // Enumerate files contained in the file catalogue.
                        foreach( var enumeratedFile in matchingFileEnumeratorEntry.Item1.EnumerateFiles( filePath, matchingFileEnumeratorEntry.Item2 ) )
                            yield return enumeratedFile;
                    }
                    else
                    // Try to get a file catalogue filter by querying each file catalogue enumerator,
                    // if it supports it.
                    {
                        matchingFileEnumeratorEntry = new Tuple<IFileCatalogueEnumerator,bool, bool>( null, false, false );
                        foreach( Tuple<IFileCatalogueEnumerator, bool, bool> fileEnumeratorEntry in fileEnumerators.Values )
                        {
                            if( fileEnumeratorEntry.Item1.SupportsPath( filePath ) )
                            {
                                matchingFileEnumeratorEntry = fileEnumeratorEntry;
                                break;
                            }
                        }
                        if( matchingFileEnumeratorEntry.Item1 != null )
                        {
                            // Enumerate file catalogue file, if requested.
                            if( !matchingFileEnumeratorEntry.Item3 )
                                yield return filePath;
                            // Enumerate files contained in the file catalogue.
                            foreach( var enumeratedFile in matchingFileEnumeratorEntry.Item1.EnumerateFiles( filePath, matchingFileEnumeratorEntry.Item2 ) )
                                yield return enumeratedFile;
                        }
                        else
                        {
                            yield return filePath;
                        }
                    }
                }
            }
        }
    }
}
