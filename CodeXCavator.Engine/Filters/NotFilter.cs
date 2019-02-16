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
    public class NotFilter : FilterChain
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public NotFilter() : this( (IEnumerable<IFileFilter>) null )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="filters">List of file filters.</param>
        public NotFilter( params IFileFilter[] filters ) : this( (IEnumerable < IFileFilter >) filters )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="filters">List of file filters.</param>
        public NotFilter( IEnumerable<IFileFilter> filters ) : base( filters )
        {
        }

        /// <summary>
        /// Filters the files using the filters in the chain and inverts results.
        /// </summary>
        /// <param name="files">Input files.</param>
        /// <returns>Difference of input set and results returned by filters in chain.</returns>
        public override IEnumerable<string> Filter(IEnumerable<string> files)
        {
 	        IEnumerable<string> output = null;
            foreach( var filter in Filters )
            {
                if( output == null )
                    output = filter.Filter( files );
                else
                {
                    output = output.Union( filter.Filter( files ) );
                }
            }
            if( output != null )
            {
                return files.Except( output );
            }

            return files;
        }
    }
}
