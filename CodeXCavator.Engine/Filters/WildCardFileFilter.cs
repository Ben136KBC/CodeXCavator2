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

namespace CodeXCavator.Engine.Filters
{
    /// <summary>
    /// RegExFileFilter configuration
    /// </summary>
    public class WildCardFileFilterConfiguration
    {
        /// <summary>
        /// Patterns
        /// </summary>
        [XmlArray("Patterns")]
        [XmlArrayItem("Pattern")]
        public string[] Patterns { get; set; }
    }

    /// <summary>
    /// WildCardFileFilter class.
    /// 
    /// The WildCardFileFilter class implements the IInvertibleFileFilter interface and is responsible for 
    /// passing thru or filter out files, which match certain wild card patterns.
    /// </summary>
    public class WildCardFileFilter : IInvertibleFileFilter, IConfigurable< WildCardFileFilterConfiguration >
    {
        /// <summary>
        /// List of wildcard patterns.
        /// 
        /// Allowed wildcards are ? (any character), * (any characters), # (numeric character). 
        /// Optionally you can specify character groups by using [characterlist] to match certain characters 
        /// or [!characterlist] to permit certain characters.
        /// 
        /// See also the Like operator of VB.NET.
        /// </summary>
        public string[] Patterns { get; set; }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public WildCardFileFilter()
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// 
        /// The default mode is inclusive.
        /// </summary>
        /// <param name="patterns">List of wildcard patterns.</param>
        public WildCardFileFilter( params string[] patterns )
        {
            Patterns = patterns;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="mode">Filter mode.</param>
        /// <param name="patterns">List of wildcard patterns.</param>
        public WildCardFileFilter( FileFilterMode mode, params string[] patterns )
        {
            Patterns = patterns;
            Mode = mode;
        }

        /// <summary>
        /// Current filter mode.
        /// </summary>
        public FileFilterMode Mode { get; set; }

        /// <summary>
        /// Filters the given list of files.
        /// 
        /// If the current mode of the filter is inclusive the filter passes thru any files, 
        /// which match at least one of the patterns contained in the list defined by the Patterns property.
        /// 
        /// If the current mode of the filter is exclusive the filter filters out all files,
        /// which match at least one of the patterns contained in the list defined by the Pattens property.
        /// </summary>
        /// <param name="files">List of files, which should be filtered.</param>
        /// <returns>Filtered list of files.</returns>
        public IEnumerable<string> Filter( IEnumerable<string> files )
        {
            return Filter( files, Mode );
        }

        /// <summary>
        /// Filters the given list of file.s
        /// 
        /// If passed mode is inclusive the filter passes thru any files, 
        /// which match at least one of the patterns contained in the list defined by the Patterns property.
        /// 
        /// If passed mode is exclusive the filter filters out all files,
        /// which match at least one of the patterns contained in the list defined by the Pattens property.
        /// </summary>
        /// <param name="files">List of files, which should be filtered.</param>
        /// <param name="mode">Filter mode.</param>
        /// <returns>Filtered list of files.</returns>
        public IEnumerable<string> Filter( IEnumerable<string> files, FileFilterMode mode )
        {
            foreach( var filePath in files )
            {
                string fileName = System.IO.Path.GetFileName( filePath );
                if( Patterns == null || Patterns.Length == 0 )
                {
                    if( mode == FileFilterMode.Inclusive )
                        yield return filePath;
                }
                else
                {
                    bool isMatch = false;
                    foreach( var pattern in Patterns )
                    {
                        if( Microsoft.VisualBasic.CompilerServices.Operators.LikeString( fileName, pattern, Microsoft.VisualBasic.CompareMethod.Text ) )
                        {
                            isMatch = true;
                            break;
                        }
                    }

                    if( isMatch ^ ( mode == FileFilterMode.Exclusive ) )
                        yield return filePath;                    
                }
            }
        }

        /// <summary>
        /// Configures the filter.
        /// </summary>
        /// <param name="configuration">Filter configuration.</param>
        public void Configure( WildCardFileFilterConfiguration configuration )
        {
            if( configuration != null )
            {
                Patterns = configuration.Patterns;
            }
        }
    }
}
