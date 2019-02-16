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

namespace CodeXCavator.Engine.Attributes
{
    /// <summary>
    /// File extensions attribute.
    /// 
    /// This attribute can be used with file catalogue enumerators and file highlighters to specify for which 
    /// file types they can be used.
    /// </summary>
    public class FileExtensionsAttribute : Attribute
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileExtensions">List of supported file extensions.</param>
        public FileExtensionsAttribute( params string[] fileExtensions )
        {
            FileExtensions = fileExtensions;
        }
        
        /// <summary>
        /// List of supported file extensions.
        /// </summary>
        public string[] FileExtensions { get; internal set; }
    }
}
