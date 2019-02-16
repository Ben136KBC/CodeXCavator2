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
    /// PassThruFileFilter class.
    /// 
    /// The class PassThruFileFilter implements the IInvertibleFileFilter interface, and either
    /// passes thru all files, or filters them out.
    /// </summary>
    public class PassThruFileFilter : IInvertibleFileFilter
    {

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PassThruFileFilter()
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="mode">Filter mode.</param>
        public PassThruFileFilter( FileFilterMode mode )
        {
            Mode = mode;
        }

        /// <summary>
        /// Current filter mode.
        /// </summary>
        public FileFilterMode Mode
        {
            get;
            set;
        }

        /// <summary>
        /// Either passes thru all files, or filters all out depending on the current filter mode.
        /// </summary>
        /// <param name="files">List of file to be filtered.</param>
        /// <returns>Filtered list of files.</returns>
        public IEnumerable<string> Filter( IEnumerable<string> files )
        {
            return Filter( files, Mode );
        }

        /// <summary>
        /// Either passes thru all files, or filters all out depending on value of mode parameter.
        /// </summary>
        /// <param name="files">List of files to be filtered.</param>
        /// <param name="mode">Filter mode.</param>
        /// <returns>Filtered list of files.</returns>
        public IEnumerable<string> Filter( IEnumerable<string> files, FileFilterMode mode )
        {
            if( mode == FileFilterMode.Inclusive )
            {
                foreach( var filePath in files )
                    yield return filePath;
            }
            else
            {
                yield break;
            }            
        }

    }
}
