// Copyright 2020 Christoph Brzozowski
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

using Lucene.Net.Index;
using System.Collections.Generic;

namespace CodeXCavator.Engine.Extensions
{
    /// <summary>
    /// Class containing extensions for the Lucene TermEnum class.
    /// </summary>
    internal static class TermEnumExtensions
    {
        /// <summary>
        /// Converts TermEnum to an enumerable of Term instances.
        /// </summary>
        /// <param name="termEnum">TermEnum instance which should be converted to an IEnumerable</param>
        /// <returns>Enumerable of Term objects.</returns>
        public static IEnumerable<Term> ToEnumerable(this TermEnum termEnum)
        {
            if( termEnum != null )
            {
                using( termEnum )
                {
                    do
                    {
                        if( termEnum.Term != null )
                            yield return termEnum.Term;
                    }
                    while( termEnum.Next() );
                }
            }
        }

        /// <summary>
        /// Converts TermEnum to an enumerable of Term instances. Stops enumeration when the Term does not belong to the specified field.
        /// </summary>
        /// <param name="termEnum">TermEnum instance which should be converted to an IEnumerable</param>
        /// <param name="field">Field which should be enumerated.</param>
        /// <returns>Enumerable of Term objects.</returns>
        public static IEnumerable<Term> ToEnumerable(this TermEnum termEnum, string field)
        {
            if( termEnum != null )
            {
                do
                {
                    Term term = termEnum.Term;
                    if (term != null )
                    {
                        if( term.Field != field )
                            break;
                        yield return term;
                    }
                }
                while( termEnum.Next() );
            }
        }

    }
}
