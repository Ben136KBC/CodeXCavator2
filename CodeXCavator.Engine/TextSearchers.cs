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
using System.Xml.Linq;
using System.Xml.Serialization;
using System.Text;
using System.Reflection;
using CodeXCavator.Engine.Interfaces;
using CodeXCavator.Engine.Highlighters;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// TextSearchers class.
    /// 
    /// The TextSearchers class is a global registry for text searchers.
    /// 
    /// You can register text searchers implementing the ITextSearcher interface.
    /// a highlighter instance matching a certain file type.
    /// </summary>
    public static class TextSearchers
    {
        private static Dictionary<string, Type> sTextSearcherTypes = new Dictionary<string, Type>();

        static TextSearchers()
        {
            RegisterDefaultTextSearchers();
        }

        /// <summary>
        /// Registers all default text searchers.
        /// </summary>
        private static void RegisterDefaultTextSearchers()
        {
            PluginManager.LoadPlugins<Interfaces.ITextSearcher>();
            RegisterTextSearcher<Searchers.LiteralTextSearcher>();
            RegisterTextSearcher<Searchers.QueryTextSearcher>();
            RegisterTextSearcher<Searchers.WildcardTextSearcher>();
            RegisterTextSearcher<Searchers.RegexTextSearcher>();
        }

        /// <summary>
        /// Registers a text searcher.
        /// </summary>
        /// <typeparam name="TEXT_SEARCHER_TYPE">Type of the text searcher, which should be registered.</typeparam>
        public static void RegisterTextSearcher<TEXT_SEARCHER_TYPE>() where TEXT_SEARCHER_TYPE: ITextSearcher, new()
        {
            RegisterTextSearcher( typeof( TEXT_SEARCHER_TYPE ) );
        }

        /// <summary>
        /// Registers a text searcher.
        /// </summary>
        /// <param name="textSearcherType">Type of the text searcher, which should be registered.</param>
        public static void RegisterTextSearcher( Type textSearcherType )
        {
            if( textSearcherType != null && typeof( ITextSearcher ).IsAssignableFrom( textSearcherType ) )
                sTextSearcherTypes[ textSearcherType.Name ] = textSearcherType;
        }

        /// <summary>
        /// Unregisters a text searcher.
        /// </summary>
        /// <typeparam name="TEXT_SEARCHER_TYPE">Type of the text searcher, which should be unregistered.</typeparam>
        public static void UnregisterTextSearcher<TEXT_SEARCHER_TYPE>() where TEXT_SEARCHER_TYPE : ITextSearcher
        {
            UnregisterTextSearcher( typeof( TEXT_SEARCHER_TYPE ) );
        }

        /// <summary>
        /// Unregisters a text searcher.
        /// </summary>
        /// <param name="textSearcherType">Text searcher type, which should be unregistered.</param>
        public static void UnregisterTextSearcher( Type textSearcherType )
        {
            if( textSearcherType != null && typeof( ITextSearcher ).IsAssignableFrom( textSearcherType ) )
                sTextSearcherTypes.Remove( textSearcherType.Name );
        }

        /// <summary>
        /// Returns all registered text searchers.
        /// 
        /// This method always creates a new instance for each returned text searcher.
        /// </summary>
        public static IEnumerable<ITextSearcher> RegisteredTextSearchers
        {
            get
            {
                foreach( var textSearcherTypeName in sTextSearcherTypes.Keys )
                    yield return CreateTextSearcher( textSearcherTypeName );
            }
        }

        /// <summary>
        /// Returns the types of the registered text searchers.
        /// </summary>
        public static IEnumerable<Type> RegisteredTextSearcherTypes
        {
            get
            {
                return sTextSearcherTypes.Values;
            }
        }

        /// <summary>
        /// Creates a text searcher.
        /// </summary>
        /// <param name="textSearcherTypeName">Type name of the text searcher which should be created.</param>
        /// <returns>Instance of the desired text searcher, or null.</returns>
        public static ITextSearcher CreateTextSearcher( string textSearcherTypeName )
        {
            Type textSearcherType = null;
            if( sTextSearcherTypes.TryGetValue( textSearcherTypeName, out textSearcherType ) )
                return Activator.CreateInstance( textSearcherType ) as ITextSearcher;
            return null;
        }
    }
}
