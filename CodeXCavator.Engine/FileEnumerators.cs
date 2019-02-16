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
    /// FileEnumerators class.
    /// 
    /// The class FileEnumerators acts as a global registry for file enumerators.
    /// 
    /// You can register file enumerators implementing the IFileEnumerator interface, and query the registry for
    /// a file enumerator matching a certain type name.
    /// </summary>
    public static class FileEnumerators
    {
        private const string XML_ELEMENT_FILE_ENUMERATOR = "Source";
        private const string XML_ELEMENT_CONFIGURATION = "Configuration";
        private const string XML_TYPE_ATTRIBUTE = "Type";

        private static Dictionary< string, Type > sFileEnumerators = new Dictionary<string, Type >( StringComparer.OrdinalIgnoreCase );

        static FileEnumerators()
        {
            RegisterDefaultFileEnumerators();
        }

        /// <summary>
        /// Registers default file enumerators.
        /// </summary>
        private static void RegisterDefaultFileEnumerators()
        {
            RegisterFileEnumerator< Enumerators.FixedFileEnumerator >();

            PluginManager.LoadPlugins<Interfaces.IFileEnumerator>();
        }

        /// <summary>
        /// Registers a file enumerator.
        /// </summary>
        /// <typeparam name="FILE_ENUMERATOR_TYPE">Type of the file enumerator, which should be registered.</typeparam>
        public static void RegisterFileEnumerator< FILE_ENUMERATOR_TYPE >() where FILE_ENUMERATOR_TYPE : IFileEnumerator, new()
        {
            RegisterFileEnumerator( typeof( FILE_ENUMERATOR_TYPE ) );
        }

        /// <summary>
        /// Registers a file enumerator.
        /// </summary>
        /// <param name="fileEnumeratorType">Type of the file enumerator, which should be registered.</param>
        public static void RegisterFileEnumerator( Type fileEnumeratorType )
        {
            if( fileEnumeratorType != null && typeof( IFileEnumerator ).IsAssignableFrom( fileEnumeratorType ) )
            {
                sFileEnumerators[ fileEnumeratorType.Name ] = fileEnumeratorType;
            }            
        }

        /// <summary>
        /// Unregisters a file enumerator.
        /// </summary>
        /// <typeparam name="FILE_ENUMERATOR_TYPE">Type of the file enumerator, which should be unregistered.</typeparam>
        public static void UnregisterFileEnumerator< FILE_ENUMERATOR_TYPE >() where FILE_ENUMERATOR_TYPE : IFileEnumerator, new()
        {
            UnregisterFileEnumerator( typeof( FILE_ENUMERATOR_TYPE ) );
        }

        /// <summary>
        /// Unregisters a file enumerator.
        /// </summary>
        /// <param name="fileEnumeratorType">Type of the file enumerator, which should be unregistered.</typeparam>
        public static void UnregisterFileEnumerator( Type fileEnumeratorType )
        {
            if( fileEnumeratorType != null && typeof( IFileEnumerator ).IsAssignableFrom( fileEnumeratorType ) )
            {
                sFileEnumerators.Remove( fileEnumeratorType.Name );
            }
        }

        /// <summary>
        /// Returns a new file enumerator of the specified type.
        /// </summary>
        /// <param name="fileEnumeratorName">Name of the type of the file enumerator type, which should be created.</param>
        /// <returns>IFileEnumerator instance of the specified type, or null if the type does not exist.</returns>
        public static IFileEnumerator CreateFileEnumerator( string fileEnumeratorName )
        {
            Type enumeratorType;
            if( sFileEnumerators.TryGetValue( fileEnumeratorName, out enumeratorType ) )
                return Activator.CreateInstance( enumeratorType ) as IFileEnumerator;
            return  null;
        }

        /// <summary>
        /// Returns a new file enumerator of the specified type and configures it.
        /// </summary>
        /// <param name="fileEnumeratorName">Name of the type of the file enumerator type, which should be created.</param>
        /// <param name="configuration">Configuration object, from which the created instance should be configured.</param>
        /// <returns>IFileEnumerator instance of the specified type, or null if the type does not exist.</returns>
        public static IFileEnumerator CreateFileEnumerator( string fileEnumeratorName, object configuration )
        {
            Type enumeratorType;
            if( configuration == null )
                return null;

            if( sFileEnumerators.TryGetValue( fileEnumeratorName, out enumeratorType ) )
                return enumeratorType.CreateAndConfigure( configuration ) as IFileEnumerator;

            return null;
        }

        /// <summary>
        /// Returns a new file enumerator of the specified type and configures it.
        /// </summary>
        /// <param name="fileEnumeratorName">Name of the type of the file enumerator type, which should be created.</param>
        /// <param name="configurationElement">Configuration xml element, from which the created instance should be configured.</param>
        /// <returns>IFileEnumerator instance of the specified type, or null if the type does not exist.</returns>
        public static IFileEnumerator CreateFileEnumerator( string fileEnumeratorName, XElement configurationElement )
        {
            Type enumeratorType;
            if( configurationElement == null )
                return null;

            if( sFileEnumerators.TryGetValue( fileEnumeratorName, out enumeratorType ) )
                return enumeratorType.CreateAndConfigure( configurationElement ) as IFileEnumerator;

            return null;
        }

        /// <summary>
        /// Returns a new file enumerator created from the file enumerator element.
        /// </summary>
        /// <param name="fileEnumeratorElement">File enumerator xml element, from which a file enumerator should be created.</param>
        /// <returns>IFileEnumerator instance created from the xml element, or null if the type does not exist.</returns>
        public static IFileEnumerator CreateFileEnumerator( XElement fileEnumeratorElement )
        {
            if( fileEnumeratorElement.Name.LocalName.Equals( XML_ELEMENT_FILE_ENUMERATOR ) )
            {
                var typeAttribute = fileEnumeratorElement.Attribute( XML_TYPE_ATTRIBUTE );
                var configurationElement = fileEnumeratorElement.Element( XML_ELEMENT_CONFIGURATION );
                if( typeAttribute != null )
                {
                    if( configurationElement != null )
                        return CreateFileEnumerator( typeAttribute.Value, configurationElement );
                    else
                        return CreateFileEnumerator( typeAttribute.Value );
                }
            }
            return null;
        }

    }
}
