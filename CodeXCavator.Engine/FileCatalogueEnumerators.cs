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
using CodeXCavator.Engine.Enumerators;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// FileCatalogueEnumerators class.
    /// 
    /// The class FileCatalogueEnumerators acts as a global registry for file catalogue enumerators.
    /// 
    /// You can register file catalogue enumerators implementing the IFileCatalogueEnumerator interface with a file extension, and query the registry for
    /// a file catalogue enumerator matching a certain file catalogue type.
    /// </summary>
    public static class FileCatalogueEnumerators
    {
        private const string XML_ELEMENT_FILE_CATALOGUE_ENUMERATOR = "Catalogue";
        private const string XML_ELEMENT_CONFIGURATION = "Configuration";
        private const string XML_TYPE_ATTRIBUTE = "Type";

        private static Dictionary< string, IFileCatalogueEnumerator > sFileCatalogueEnumerators = new Dictionary<string, IFileCatalogueEnumerator >( StringComparer.OrdinalIgnoreCase );
        private static Dictionary< string, Type > sFileCatalogueEnumeratorTypes = new Dictionary<string,Type>();

        static FileCatalogueEnumerators()
        {
            RegisterDefaultFileCatalogueEnumerators();
        }

        /// <summary>
        /// Registers default file catalogue enumerators.
        /// </summary>
        private static void RegisterDefaultFileCatalogueEnumerators()
        {
            RegisterFileCatalogueEnumerator<DirectoryFileEnumerator>();
            PluginManager.LoadPlugins<Interfaces.IFileCatalogueEnumerator>();
        }

        /// <summary>
        /// Registers a file catalogue enumerator.
        /// </summary>
        /// <typeparam name="FILECATALOGUE_ENUMERATOR_TYPE">Type of the file catalogue enumerator, which should be registered.</typeparam>
        /// <param name="extensions">List of file extensions, for which the file catalogue enumerator can be applied.</param>
        public static void RegisterFileCatalogueEnumerator< FILECATALOGUE_ENUMERATOR_TYPE >( params string[] extensions ) where FILECATALOGUE_ENUMERATOR_TYPE : IFileCatalogueEnumerator, new()
        {
            sFileCatalogueEnumeratorTypes[ typeof( FILECATALOGUE_ENUMERATOR_TYPE ).Name ] = typeof( FILECATALOGUE_ENUMERATOR_TYPE );
            RegisterFileCatalogueEnumerator( new FILECATALOGUE_ENUMERATOR_TYPE(), extensions );
        }

        /// <summary>
        /// Registers a file catalogue enumerator.
        /// </summary>
        /// <param name="fileCatalogueEnumeratorType">Type of the file catalogue enumerator, which should be registered.</param>
        /// <param name="extensions">List of file extensions, for which the file catalogue enumerator can be applied.</param>
        public static void RegisterFileCatalogueEnumerator( Type fileCatalogueEnumeratorType, params string[] extensions )
        {
            if( fileCatalogueEnumeratorType != null && typeof( IFileCatalogueEnumerator ).IsAssignableFrom( fileCatalogueEnumeratorType ) )
            {
                sFileCatalogueEnumeratorTypes[ fileCatalogueEnumeratorType.Name ] = fileCatalogueEnumeratorType;
                var fileExtensionsAttribute = fileCatalogueEnumeratorType.GetCustomAttributes(true).Where( attribute => typeof( Attributes.FileExtensionsAttribute ).IsAssignableFrom( attribute.GetType() ) ).Cast< Attributes.FileExtensionsAttribute >().FirstOrDefault();
                if( fileExtensionsAttribute != null && extensions.Length == 0 )
                    RegisterFileCatalogueEnumerator( Activator.CreateInstance( fileCatalogueEnumeratorType ) as IFileCatalogueEnumerator, fileExtensionsAttribute.FileExtensions );
                else
                    RegisterFileCatalogueEnumerator( Activator.CreateInstance( fileCatalogueEnumeratorType ) as IFileCatalogueEnumerator, extensions );
            }            
        }

        /// <summary>
        /// Registers a file catalogue enumerator.
        /// </summary>
        /// <param name="enumerator">File catalogue enumerator instance, which should be registered.</param>
        /// <param name="extensions">List of file extensions, for which the file catalogue enumerator can be applied.</param>
        public static void RegisterFileCatalogueEnumerator( IFileCatalogueEnumerator enumerator, params string[] extensions )
        {
            if( enumerator == null )
                return;

            sFileCatalogueEnumeratorTypes[enumerator.GetType().Name] = enumerator.GetType();

            foreach( var extension in extensions )
                sFileCatalogueEnumerators[extension] = enumerator;
        }

        /// <summary>
        /// Unregisters a file catalogue enumerator.
        /// </summary>
        /// <param name="extensions">List of file extensions, for which the registered file catalogue enumerator should be unregistered.</param>
        public static void UnregisterFileCatalogueEnumerator( params string[] extensions )
        {
            foreach( var extension in extensions )
            {
                IFileCatalogueEnumerator fileCatalogueEnumerator;
                if( sFileCatalogueEnumerators.TryGetValue( extension, out fileCatalogueEnumerator ) )
                    sFileCatalogueEnumeratorTypes.Remove( fileCatalogueEnumerator.GetType().Name );
                sFileCatalogueEnumerators.Remove( extension );
            }
        }

        /// <summary>
        /// Unregisters a file catalogue enumerator.
        /// </summary>
        /// <param name="enumerator">Instance of the file catalogue enumerator, which should be unregistered.</param>
        public static void UnregisterFileCatalogueEnumerator( IFileCatalogueEnumerator enumerator )
        {
            if( enumerator != null )
                sFileCatalogueEnumeratorTypes.Remove( enumerator.GetType().Name );

            foreach( var extensionToUnregister in sFileCatalogueEnumerators.Where( registeredFileEnumerator => registeredFileEnumerator.Value == enumerator ).Select( registeredFileEnumerator => registeredFileEnumerator.Key ).ToList() ) 
                sFileCatalogueEnumerators.Remove( extensionToUnregister ); 
        }

        /// <summary>
        /// Returns a file catalogue enumerator supporting enumerating the specified file catalogue type.
        /// </summary>
        /// <param name="fileExtension">File extension of the file catalogue, for which a matching file catalogue enumerator should be found.</param>
        /// <returns>IFileCatalogueEnumerator instance capable of enumerating the specified file catalogue type, or null, if non was found.</returns>
        public static IFileCatalogueEnumerator GetFileCatalogueEnumerator( string fileExtension )
        {
            IFileCatalogueEnumerator enumerator;
            if( sFileCatalogueEnumerators.TryGetValue( fileExtension, out enumerator ) )
                return enumerator;
            return  null;
        }

        /// <summary>
        /// Returns a file catalogue enumerator type.
        /// </summary>
        /// <param name="fileCatalogueEnumeratorType">Type of the file catalogue enumerator to be retrieved.</param>
        /// <returns>Type of specified file catalogue enumerator, or null, if the type is not registered.</returns>
        public static Type GetFileCatalogueEnumeratorType( string fileCatalogueEnumeratorType )
        {
            Type enumeratorType;
            if( sFileCatalogueEnumeratorTypes.TryGetValue( fileCatalogueEnumeratorType, out enumeratorType ) )
                return enumeratorType;
            return null;
        }

        /// <summary>
        /// Returns a file catalogue enumerator supporting enumerating the specified file catalogue.
        /// </summary>
        /// <param name="fileCataloguePath">Path of the file catalogue file, for which a matching file catalogue enumerator should be found.</param>
        /// <returns>IFileCatalogueEnumerator instance capable of enumerating the specified file catalogue, or null, if non was found.</returns>
        public static IFileCatalogueEnumerator GetFileCatalogueEnumeratorForPath( string fileCataloguePath )
        {
            // Try by extension first...
            string extension = System.IO.Path.GetExtension( fileCataloguePath );
            IFileCatalogueEnumerator matchingEnumerator;
            if( sFileCatalogueEnumerators.TryGetValue( extension, out matchingEnumerator ) )
                if( matchingEnumerator.SupportsPath( fileCataloguePath ) )
                    return matchingEnumerator;
            
            // Iterate over all registered file catalogue enumerators and query each, whether it supports the path.
            foreach( var enumerator in sFileCatalogueEnumerators )
            {
                if( enumerator.Value.SupportsPath( fileCataloguePath ) )
                    return enumerator.Value;
            }

            // No matching file catalogue enumerator found.
            return null;
        }

        /// <summary>
        /// Returns a file catalogue enumerator type supporting enumerating the specified file catalogue.
        /// </summary>
        /// <param name="fileCataloguePath">Path of the file catalogue file, for which a matching file catalogue enumerator should be found.</param>
        /// <returns>File catalogue enumerator type capable of enumerating the specified file catalogue, or null, if non was found.</returns>
        public static Type GetFileCatalogueEnumeratorTypeForPath( string fileCataloguePath )
        {
            // Try by instance first
            var enumerator = GetFileCatalogueEnumeratorForPath( fileCataloguePath );
            if( enumerator != null )
                return enumerator.GetType();

            // Iterate over all registered file catalogue enumerator types
            foreach( var enumeratorType in sFileCatalogueEnumeratorTypes.Values )
            {
                try
                {
                    var fileCatalogueEnumerator = Activator.CreateInstance( enumeratorType ) as IFileCatalogueEnumerator;
                    if( fileCatalogueEnumerator.SupportsPath( fileCataloguePath ) )
                        return enumeratorType;
                }
                catch
                {
                }
            }

            // No matching file catalogue enumerator found.
            return null;            
        }

        /// <summary>
        /// Returns a new file catalogue enumerator of the specified type.
        /// </summary>
        /// <param name="fileCatalogueEnumeratorName">Name of the type of the file catalogue enumerator type, which should be created.</param>
        /// <returns>IFileCatalogueEnumerator instance of the specified type, or null if the type does not exist.</returns>
        public static IFileCatalogueEnumerator CreateFileCatalogueEnumerator( string fileCatalogueEnumeratorName )
        {
            Type enumeratorType;
            if( sFileCatalogueEnumeratorTypes.TryGetValue( fileCatalogueEnumeratorName, out enumeratorType ) )
                return Activator.CreateInstance( enumeratorType ) as IFileCatalogueEnumerator;
            return  null;
        }

        /// <summary>
        /// Returns a new file catalogue enumerator of the specified type and configures it.
        /// </summary>
        /// <param name="fileCatalogueEnumeratorName">Name of the type of the file catalogue enumerator type, which should be created.</param>
        /// <param name="configuration">Configuration object, from which the created instance should be configured.</param>
        /// <returns>IFileCatalogueEnumerator instance of the specified type, or null if the type does not exist.</returns>
        public static IFileCatalogueEnumerator CreateFileCatalogueEnumerator( string fileCatalogueEnumeratorName, object configuration )
        {
            Type enumeratorType;
            if( configuration == null )
                return null;

            if( sFileCatalogueEnumeratorTypes.TryGetValue( fileCatalogueEnumeratorName, out enumeratorType ) )
                return enumeratorType.CreateAndConfigure( configuration ) as IFileCatalogueEnumerator;

            return null;
        }

        /// <summary>
        /// Returns a new file catalogue enumerator of the specified type and configures it.
        /// </summary>
        /// <param name="fileCatalogueEnumeratorName">Name of the type of the file catalogue enumerator type, which should be created.</param>
        /// <param name="configurationElement">Configuration xml element, from which the created instance should be configured.</param>
        /// <returns>IFileCatalogueEnumerator instance of the specified type, or null if the type does not exist.</returns>
        public static IFileCatalogueEnumerator CreateFileCatalogueEnumerator( string fileCatalogueEnumeratorName, XElement configurationElement )
        {
            Type enumeratorType;
            if( configurationElement == null )
                return null;

            if( sFileCatalogueEnumeratorTypes.TryGetValue( fileCatalogueEnumeratorName, out enumeratorType ) )
                return enumeratorType.CreateAndConfigure( configurationElement ) as IFileCatalogueEnumerator;

            return null;
        }

        /// <summary>
        /// Returns a new file enumerator created from the file enumerator element.
        /// </summary>
        /// <param name="fileEnumeratorElement">File enumerator xml element, from which a file enumerator should be created.</param>
        /// <returns>IFileCatalogueEnumerator instance created from the xml element, or null if the type does not exist.</returns>
        public static IFileCatalogueEnumerator CreateFileCatalogueEnumerator( XElement fileEnumeratorElement )
        {
            if( fileEnumeratorElement.Name.LocalName.Equals( XML_ELEMENT_FILE_CATALOGUE_ENUMERATOR ) )
            {
                var typeAttribute = fileEnumeratorElement.Attribute( XML_TYPE_ATTRIBUTE );
                var configurationElement = fileEnumeratorElement.Element( XML_ELEMENT_CONFIGURATION );
                if( typeAttribute != null )
                {
                    if( configurationElement != null )
                        return CreateFileCatalogueEnumerator( typeAttribute.Value, configurationElement );
                    else
                        return CreateFileCatalogueEnumerator( typeAttribute.Value );
                }
            }
            return null;
        }

    }
}
