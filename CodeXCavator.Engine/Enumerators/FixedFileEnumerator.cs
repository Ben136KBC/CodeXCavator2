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
using System.Xml.Serialization;
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.Engine.Enumerators
{
    /// <summary>
    /// Fixed file enumerator configuration class.
    /// </summary>
    public class FixedFileEnumeratorConfiguration
    {
        [XmlArray( "Files" )]
        [XmlArrayItem( "File" )]
        public string[] Files { get; set; }
    }

    /// <summary>
    /// FixedFileEnumerator class.
    /// 
    /// The FixedFileEnumerator class is responsible for enumerating a fixed list of files.
    /// </summary>
    public class FixedFileEnumerator : IFileEnumerator, IConfigurable< FixedFileEnumeratorConfiguration >
    {
        HashSet<string> mFiles = new HashSet<string>();

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FixedFileEnumerator()
        {
        }

        /// <summary>
        /// Configuration constructor.
        /// </summary>
        /// <param name="files">List of files, which should be enumerated by the fixed file enumerator.</param>
        public FixedFileEnumerator( params string[] files ) : this( (IEnumerable<string>) files )
        {
        }
        
        /// <summary>
        /// Configuration constructor.
        /// </summary>
        /// <param name="files">List of files, which should be enumerated by the fixed file enumerator.</param>
        public FixedFileEnumerator( IEnumerable<string> files )
        {
            foreach( var filePath in files )
                mFiles.Add( filePath );
        }

        /// <summary>
        /// List of files which should be enumerated.
        /// </summary>
        IEnumerable<string> Files
        {
            get { return mFiles; }
            set { mFiles = value != null ? new HashSet<string>( value ) : new HashSet<string>(); }
        }

        /// <summary>
        /// Enumerates the files defined by the Files property.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> EnumerateFiles()
        {
            return mFiles.Select( fileName => System.IO.Path.GetFullPath( Environment.ExpandEnvironmentVariables( fileName ) ) );
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
        /// Returns the file eumerator.
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return EnumerateFiles().GetEnumerator();
        }

        /// <summary>
        /// Configures the enumerator.
        /// </summary>
        /// <param name="configuration">Enumerator configuration.</param>
        public void Configure( FixedFileEnumeratorConfiguration configuration )
        {
            if( configuration != null )
            {
                Files = new HashSet<string>( configuration.Files );
            }
        }
    }
}
