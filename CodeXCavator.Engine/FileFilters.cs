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
using System.Xml.Linq;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// FileFilters class.
    /// 
    /// The class FileFilters acts as a global registry for file filters.
    /// 
    /// You can register file filter implementing the IFileFilter interface, and query the registry for
    /// a file filter matching a certain type name.
    /// </summary>
    public static class FileFilters
    {
        private const string XML_ELEMENT_FILE_FILTER = "Filter";
        private const string XML_ELEMENT_CONFIGURATION = "Configuration";
        private const string XML_TYPE_ATTRIBUTE = "Type";

        private static Dictionary< string, Type > sFileFilters = new Dictionary<string, Type >( StringComparer.OrdinalIgnoreCase );

        static FileFilters()
        {
            RegisterDefaultFileFilters();
        }

        /// <summary>
        /// Registers all default file filters.
        /// </summary>
        private static void RegisterDefaultFileFilters()
        {
            RegisterFileFilter<Filters.WildCardFileFilter>();
            RegisterFileFilter<Filters.RegExFileFilter>();
            RegisterFileFilter<Filters.OrFilter>();
            RegisterFileFilter<Filters.AndFilter>();
            RegisterFileFilter<Filters.NotFilter>();
            PluginManager.LoadPlugins<Interfaces.IFileFilter>();
        }

        /// <summary>
        /// Registers a file filter.
        /// </summary>
        /// <typeparam name="FILE_FILTER_TYPE">Type of the file filter, which should be registered.</typeparam>
        public static void RegisterFileFilter< FILE_FILTER_TYPE >() where FILE_FILTER_TYPE : IFileFilter, new()
        {
            RegisterFileFilter( typeof( FILE_FILTER_TYPE ) );
        }

        /// <summary>
        /// Registers a file filter.
        /// </summary>
        /// <param name="fileFilterTypeType">Type of the file filter, which should be registered.</param>
        public static void RegisterFileFilter( Type fileFilterType )
        {
            if( fileFilterType != null && typeof( IFileFilter ).IsAssignableFrom( fileFilterType ) )
            {
                sFileFilters[ fileFilterType.Name ] = fileFilterType;
            }            
        }

        /// <summary>
        /// Unregisters a file filter.
        /// </summary>
        /// <typeparam name="FILE_FILTER_TYPE">Type of the file filter, which should be unregistered.</typeparam>
        public static void UnregisterFileEnumerator< FILE_FILTER_TYPE >() where FILE_FILTER_TYPE : IFileFilter, new()
        {
            UnregisterFileEnumerator( typeof( FILE_FILTER_TYPE ) );
        }

        /// <summary>
        /// Unregisters a file filter.
        /// </summary>
        /// <param name="fileFilterType">Type of the file filter, which should be unregistered.</typeparam>
        public static void UnregisterFileEnumerator( Type fileFilterType)
        {
            if( fileFilterType != null && typeof( IFileFilter ).IsAssignableFrom( fileFilterType ) )
            {
                sFileFilters.Remove( fileFilterType.Name );
            }
        }

        /// <summary>
        /// Returns a new file filter of the specified type.
        /// </summary>
        /// <param name="fileFilterName">Name of the type of the file filter type, which should be created.</param>
        /// <returns>IFileFilter instance of the specified type, or null if the type does not exist.</returns>
        public static IFileFilter CreateFileFilter( string fileFilterName )
        {
            Type filterType;
            if( sFileFilters.TryGetValue( fileFilterName, out filterType ) )
                return Activator.CreateInstance( filterType ) as IFileFilter;
            return  null;
        }

        /// <summary>
        /// Returns a new file filter of the specified type and configures it.
        /// </summary>
        /// <param name="fileFilterName">Name of the type of the file filter type, which should be created.</param>
        /// <param name="configuration">Configuration object, from which the created instance should be configured.</param>
        /// <returns>IFileFilter instance of the specified type, or null if the type does not exist.</returns>
        public static IFileFilter CreateFileFilter( string fileFilterName, object configuration )
        {
            Type filterType;
            if( configuration == null )
                return null;

            if( sFileFilters.TryGetValue( fileFilterName, out filterType ) )
                return filterType.CreateAndConfigure( configuration ) as IFileFilter;

            return null;
        }

        /// <summary>
        /// Returns a new file filter of the specified type and configures it.
        /// </summary>
        /// <param name="fileFilterName">Name of the type of the file filter type, which should be created.</param>
        /// <param name="configurationElement">Configuration xml element, from which the created instance should be configured.</param>
        /// <returns>IFileFilter instance of the specified type, or null if the type does not exist.</returns>
        public static IFileFilter CreateFileFilter( string fileFilterName, XElement configurationElement )
        {
            Type filterType;
            if( configurationElement == null )
                return null;

            if( sFileFilters.TryGetValue( fileFilterName, out filterType ) )
                return filterType.CreateAndConfigure( configurationElement ) as IFileFilter;

            return null;
        }

        /// <summary>
        /// Returns a new file filter created from the file filter element.
        /// </summary>
        /// <param name="fileFilterElement">File enumerator xml element, from which a file filter should be created.</param>
        /// <returns>IFileFilter instance created from the xml element, or null if the type does not exist.</returns>
        public static IFileFilter CreateFileFilter( XElement fileFilterElement )
        {
            if( fileFilterElement.Name.LocalName.Equals( XML_ELEMENT_FILE_FILTER ) )
            {
                var typeAttribute = fileFilterElement.Attribute( XML_TYPE_ATTRIBUTE );
                var configurationElement = fileFilterElement.Element( XML_ELEMENT_CONFIGURATION );
                if( typeAttribute != null )
                {
                    if( configurationElement != null )
                        return CreateFileFilter( typeAttribute.Value, configurationElement );
                    else
                        return CreateFileFilter( typeAttribute.Value );
                }
            }
            return null;
        }

    }
}
