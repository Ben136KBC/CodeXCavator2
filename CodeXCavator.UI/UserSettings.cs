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

//This file has been modified by Ben van der Merwe

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeXCavator.Engine.Interfaces;

namespace CodeXCavator.UI
{
    /// <summary>
    /// User settings object
    /// </summary>
    public class UserSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public UserSettings()
        {
            LastSelectedIndex = null;
            SearchProcessors = new Dictionary<string, SearchProcessorSettings>();
            SearchProcessors.Add( SearchType.Contents, new SearchProcessorSettings {ImmediateSearch = false } );
            SearchProcessors.Add( SearchType.Files, new SearchProcessorSettings { ImmediateSearch = true } );
            SearchProcessors.Add( SearchType.Tags, new SearchProcessorSettings { ImmediateSearch = true } );
        }

        /// <summary>
        /// Search processor settings object
        /// </summary>
        public class SearchProcessorSettings
        {
            private Dictionary<string,object> mSearchOptions = new Dictionary<string,object>(); 
            /// <summary>
            /// Live search
            /// </summary>
            public bool ImmediateSearch { get; set; }
            /// <summary>
            /// Additional search options
            /// </summary>
            public IDictionary<string, object> SearchOptions { get { return mSearchOptions; } }

        }

        /// <summary>
        /// Index, which has been used the last time.
        /// </summary>
        public string LastSelectedIndex { get; set; }

        /// <summary>
        /// Last XML index files opened
        /// </summary>
        public string MRUFiles { get; set; }

        /// <summary>
        /// Reopen last XML index file opened on next startup true or false
        /// </summary>
        public string ReOpenLastFile { get; set; }

        /// <summary>
        /// Individual search processor settings
        /// </summary>
        public IDictionary<string, SearchProcessorSettings> SearchProcessors { get; private set; }

    }
}
