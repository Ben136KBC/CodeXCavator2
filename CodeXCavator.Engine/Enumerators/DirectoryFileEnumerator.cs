﻿// Copyright 2014 Christoph Brzozowski
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
using System.IO;
using System.Linq;
using System.Text;
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.Engine.Enumerators
{
    /// <summary>
    /// Directory file enumerator class.
    /// 
    /// The directory file enumerator implements the IFileCatalogueEnumerator interface and
    /// is responsible for enumerating files contained in a directory.
    /// </summary>
    public class DirectoryFileEnumerator : IFileCatalogueEnumerator
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public DirectoryFileEnumerator()
        {
        }

        /// <summary>
        /// Configruation constructor.
        /// </summary>
        /// <param name="fileCataloguePath">Path of the directory, whose content should be enumerated.</param>
        /// <param name="recursive">Indicates, whether the content should be enumerated recursively, or not.</param>
        public DirectoryFileEnumerator( string fileCataloguePath, bool recursive )
        {
            FileCataloguePath = fileCataloguePath;
            Recursive = recursive;
        }

        /// <summary>
        /// Path of the directory which should be enumerated.
        /// </summary>
        public string FileCataloguePath
        {
            get;
            set;
        }

        /// <summary>
        /// Indicates, whether the content should be enumerated recursively, or not.
        /// </summary>
        public bool Recursive
        {
            get;
            set;
        }

        /// <summary>
        /// Enumerates the files contained in the directory defined by FileCataloguePath, either recursively, or not,
        /// depending on the value of the Recursive property.
        /// </summary>
        /// <returns>File enumerable.</returns>
        public IEnumerable<string> EnumerateFiles()
        {
            return EnumerateFiles( FileCataloguePath, Recursive );
        }

        /// <summary>
        /// Enumerates the files contained in the specified directory.
        /// </summary>
        /// <param name="fileCataloguePath">Directory, whose files should be enumerated.</param>
        /// <param name="recursive">Indicates, whether the content should be enumerated recursively, or not.</param>
        /// <returns>File enumerable.</returns>
        public IEnumerable<string> EnumerateFiles( string fileCataloguePath, bool recursive )
        {
            IEnumerable<string> files = null;
            files = System.IO.Directory.EnumerateFiles( Environment.ExpandEnvironmentVariables(fileCataloguePath), "*", System.IO.SearchOption.AllDirectories );
            if( files != null )
            {
                var fileEnumerator = files.GetEnumerator();
                while(true)
                {
                    string absoluteFileName = null;
                    try
                    {
                        bool hasFile = fileEnumerator.MoveNext();
                        if( !hasFile )
                            break;
                        var fileName = fileEnumerator.Current;
                        absoluteFileName = System.IO.Path.GetFullPath(fileName);
                    }
                    catch( PathTooLongException )
                    {
                        absoluteFileName = null;
                    }
                    catch
                    {
                        break;
                    }
                    if( absoluteFileName != null )
                        yield return absoluteFileName;
                }
            }
        }

        /// <summary>
        /// Returns the file enumerator.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<string> GetEnumerator()
        {
            return EnumerateFiles().GetEnumerator();
        }

        /// <summary>
        /// Returns the file enumerator.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return EnumerateFiles().GetEnumerator();
        }

        /// <summary>
        /// Returns, whether the file catalogue enumerator, supports enumerating the specified file catalogue.
        /// </summary>
        /// <param name="fileCataloguePath">Path of the file catalogue, which should be checked.</param>
        /// <returns>True, if the DirectoryFileEnumerator supports enumerating the file catalogue, false otherwise.</returns>
        public bool SupportsPath( string fileCataloguePath )
        {
            return System.IO.Directory.Exists( fileCataloguePath );
        }
    }
}
