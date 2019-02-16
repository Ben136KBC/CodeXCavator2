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
    /// FilterChain class.
    /// 
    /// The FilterChain class implementes the IFilterChain interface and is responsible 
    /// for filtering a list of files thorugh a chain of file filters.
    /// </summary>
    public class FilterChain : IFilterChain
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public FilterChain() : this( (IEnumerable<IFileFilter>) null )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="filters">List of file filters.</param>
        public FilterChain( params IFileFilter[] filters ) : this( (IEnumerable < IFileFilter >) filters )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="filters">List of file filters.</param>
        public FilterChain( IEnumerable<IFileFilter> filters )
        {
            if( filters != null )
                Filters = new List<IFileFilter>( filters );
            else
                Filters = new List<IFileFilter>();
        }

        /// <summary>
        /// List of filters to be used by the filter chain.
        /// </summary>
        public IList<IFileFilter> Filters
        {
            get;
            private set;
        }

        /// <summary>
        /// Filters the list of files through the filter chain.
        /// 
        /// The method subsequently filters the list of files by each filter in the 
        /// filter list.
        /// </summary>
        /// <param name="files">Initial list of files, which should be filtered through the filter chain.</param>
        /// <returns>Filtered list of files.</returns>
        public virtual IEnumerable<string> Filter( IEnumerable<string> files )
        {
            IEnumerable< string > currentOutput = files;
            foreach( var filter in Filters )
                currentOutput = filter.Filter( currentOutput );
            foreach( var file in currentOutput )
                yield return file;
        }
    }
}
