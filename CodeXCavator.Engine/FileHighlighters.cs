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
    /// FileHighlighters class.
    /// 
    /// The FileHighlighters class is a global registry for file highlighers.
    /// 
    /// You can register highlighters implementing the IHighlighter interface with a file extension, and query the registry for
    /// a highlighter instance matching a certain file type.
    /// </summary>
    public static class FileHighlighters
    {
        private static Dictionary<string, IHighlighter> sFileHighlighters = new Dictionary<string, IHighlighter>( StringComparer.OrdinalIgnoreCase );
        private static Dictionary<string, Type> sFileHighlighterTypes = new Dictionary<string,Type>();

        private const string HIGHLIGHTERS_NAMESPACE = "CodeXCavator.Engine.Highlighters.";
        private const string CONFIGURE_METHOD_NAME = "Configure";
        private const string XML_ELEMENT_HIGHLIGHTERS = "Highlighters";
        private const string XML_ELEMENT_HIGHLIGHTER = "Highlighter";
        private const string XML_TYPE_ATTRIBUTE = "Type";
        private const string XML_FILE_EXTENSIONS_ATTRIBUTE = "FileExtensions";
        private const string XML_ELEMENT_CONFIGURATION = "Configuration";

        static FileHighlighters()
        {
            RegisterDefaultHighlighters();
        }

        /// <summary>
        /// Returns the path to the highlighter configuration file.
        /// </summary>
        private static string GetHighlighterDirectory()
        {
            return System.IO.Path.Combine( System.IO.Path.GetDirectoryName( Assembly.GetEntryAssembly().Location ), "Highlighters" );
        }

        /// <summary>
        /// Enumerates all available highlighter files.
        /// </summary>
        /// <returns>Enumeration of highlighter files.</returns>
        private static IEnumerable<string> EnumerateHighlighterFilesInDirectory( string directory )
        {
            return System.IO.Directory.EnumerateFiles( directory, "*.xml" );
        }

        /// <summary>
        /// Registers all default highlighters including highlighters defined in the "Highlighters.xml" file located in the "Configuration" directory.
        /// </summary>
        private static void RegisterDefaultHighlighters()
        {
            PluginManager.LoadPlugins<Interfaces.IHighlighter>();
            RegisterHighlightersInDirectory( GetHighlighterDirectory() );
        }

        /// <summary>
        /// Registers all highlighters contained in the specified directory.
        /// </summary>
        /// <param name="highlighterDirectory">Path to the highlighter directory, containing highlighter configuration files.</param>
        public static void RegisterHighlightersInDirectory( string highlighterDirectory )
        {
            foreach( var highlighterConfigurationFile in EnumerateHighlighterFilesInDirectory( highlighterDirectory ) )
                RegisterHighlighters( XDocument.Load( highlighterConfigurationFile ) );
        }

        /// <summary>
        /// Registers all highlighters defined in the specified highlighter configuration file.
        /// </summary>
        /// <param name="highlighterConfigurationFile">Path to the highlighter configuration file, from which the highlighters should be registered.</param>
        public static void RegisterHighlightersFromFile( string highlighterConfigurationFile )
        {
            RegisterHighlighters( XDocument.Load( highlighterConfigurationFile ) );
        }

        /// <summary>
        /// Registers all highlighters defined in the specified Xml configuration.
        /// </summary>
        /// <param name="highlighterXmlConfiguration">XDocument containing highlighter definitions.</param>
        public static void RegisterHighlighters( XDocument highlighterXmlConfiguration )
        {
            if( highlighterXmlConfiguration != null )
            {
                if( highlighterXmlConfiguration.Root.Name.LocalName.Equals( XML_ELEMENT_HIGHLIGHTERS, StringComparison.OrdinalIgnoreCase ) )
                {
                    foreach( var highlighterElement in highlighterXmlConfiguration.Root.Elements().Where( element => element.Name.LocalName.Equals( XML_ELEMENT_HIGHLIGHTER, StringComparison.OrdinalIgnoreCase ) ) )
                        RegisterHighlighter( highlighterElement );
                }
            }
        }

        /// <summary>
        /// Returns the highlighter type corresponding to the highlighter.
        /// </summary>
        /// <param name="highlighterTypeName">Type name of the highlighter.</param>
        /// <returns>Type object corresponding to the specified highlighter type name or null, if not found.</returns>
        private static Type GetHighlighterType( string highlighterTypeName )
        {
            Type highlighterType = Type.GetType( highlighterTypeName );
            if( highlighterType != null )
                return highlighterType;
            highlighterType = Type.GetType( HIGHLIGHTERS_NAMESPACE + highlighterTypeName );
            if( highlighterType != null )
                return highlighterType;
            if( sFileHighlighterTypes.TryGetValue( highlighterTypeName, out highlighterType ) )
                return highlighterType;
            return null;
        }

        /// <summary>
        /// Returns the configuration object type for the specified highlighter type.
        /// 
        /// This method assumes that the highlighter derives from the IConfigurable<> interface. The generic parameter
        /// is assumed to be the configuration type of the highlighter.
        /// </summary>
        /// <param name="highlighterType">Highlighter type for which the configuration type should be determined.</param>
        /// <returns>Type object for the highlighter configuration object, or null, if highlighter is not configurable.</returns>
        private static Type GetHighlighterConfigurationType( Type highlighterType )
        {
            if( highlighterType != null )
            {
                foreach( var implementedInterface in highlighterType.GetInterfaces() )
                {
                    if( implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == typeof( IConfigurable<> ) )
                    {
                        return implementedInterface.GetGenericArguments()[0];
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Registers a highlighter whose configuration is given by the specified XmlElement
        /// </summary>
        /// <param name="highlighterElement">Highlighter root element.</param>
        private static void RegisterHighlighter( XElement highlighterElement )
        {
            if( highlighterElement != null )
            {
                XAttribute highlighterTypeAttribute = highlighterElement.Attribute( XML_TYPE_ATTRIBUTE );
                XAttribute fileExtensionsAttribute = highlighterElement.Attribute( XML_FILE_EXTENSIONS_ATTRIBUTE );
                string highlighterTypeName = highlighterTypeAttribute != null ? highlighterTypeAttribute.Value : null;
                string fileExtensionList = fileExtensionsAttribute != null ? fileExtensionsAttribute.Value : null;

                if( !string.IsNullOrEmpty( highlighterTypeName ) && !string.IsNullOrEmpty( fileExtensionList ) )
                {
                    Type highlighterType = GetHighlighterType( highlighterTypeName );
                    Type highlighterConfigurationType = GetHighlighterConfigurationType( highlighterType );

                    object highlighterConfiguration = LoadHighlighterConfiguration( highlighterElement, highlighterConfigurationType );

                    if( highlighterType != null )
                    {
                        try
                        {
                            Interfaces.IHighlighter highlighter = Activator.CreateInstance( highlighterType ) as Interfaces.IHighlighter;
                            if( highlighter != null && highlighterConfiguration != null )
                                ConfigureHighlighter( highlighter, highlighterConfiguration );
                            RegisterFileHighlighter( highlighter, fileExtensionList.Split( ',', ';' ) );
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads a highlighter configuration.
        /// </summary>
        /// <param name="highlighterElement">Highlighter root element.</param>
        /// <param name="highlighterConfigurationType">Highlighter configuration type.</param>
        /// <returns>Configuration object loaded from the Configuration element below the highlighter root element.</returns>
        private static object LoadHighlighterConfiguration( XElement highlighterElement, Type highlighterConfigurationType )
        {
            try
            {
                XElement configurationElement = highlighterElement.Element( XML_ELEMENT_CONFIGURATION );
                if( configurationElement != null )
                {
                    XmlSerializer configurationSerializer = new XmlSerializer( highlighterConfigurationType, new XmlRootAttribute( XML_ELEMENT_CONFIGURATION ) );
                    var configuration = configurationSerializer.Deserialize( configurationElement.CreateReader() );
                    return configuration;
                }
            }
            catch
            {
            }
            return null;
        }
        
        /// <summary>
        /// Configures the specified highlighter according to the specified highlighter configuration.
        /// </summary>
        /// <param name="highlighter">Highlighter, which should be configured.</param>
        /// <param name="highlighterConfiguration">Highlighter configuration.</param>
        private static void ConfigureHighlighter( IHighlighter highlighter, object highlighterConfiguration )
        {
            if( highlighter != null )
            {
                try
                {
                    var configureMethod = highlighter.GetType().GetMethod( CONFIGURE_METHOD_NAME );
                    if( configureMethod != null )
                        configureMethod.Invoke( highlighter, new object[] { highlighterConfiguration } );
                }
                catch
                {
                }                
            }
        }

        /// <summary>
        /// Registers a highlighter.
        /// </summary>
        /// <typeparam name="FILE_HIGHLIGHTER_TYPE">Type of the file highligher, which should be registered.</typeparam>
        /// <param name="extensions">File extensions the file highighter applies to.</param>
        public static void RegisterFileHighlighter<FILE_HIGHLIGHTER_TYPE>( params string[] extensions ) where FILE_HIGHLIGHTER_TYPE : IHighlighter, new()
        {
            RegisterFileHighlighter( new FILE_HIGHLIGHTER_TYPE(), extensions );
        }

        /// <summary>
        /// Registers a highlighter.
        /// </summary>
        /// <param name="fileHighlighterType">Type of the file highligher, which should be registered.</param>
        /// <param name="extensions">File extensions the file highighter applies to.</param>
        public static void RegisterFileHighlighter( Type fileHighlighterType, params string[] extensions )
        {
            if( fileHighlighterType != null && typeof( IHighlighter ).IsAssignableFrom( fileHighlighterType ) )
            {
                sFileHighlighterTypes[ fileHighlighterType.Name ] = fileHighlighterType;
                var fileExtensionsAttribute = fileHighlighterType.GetCustomAttributes(true).Where( attribute => typeof( Attributes.FileExtensionsAttribute ).IsAssignableFrom( attribute.GetType() ) ).Cast< Attributes.FileExtensionsAttribute >().FirstOrDefault();
                if( fileExtensionsAttribute != null && extensions.Length == 0 )
                    RegisterFileHighlighter( Activator.CreateInstance( fileHighlighterType ) as IHighlighter, fileExtensionsAttribute.FileExtensions );
                else
                    RegisterFileHighlighter( Activator.CreateInstance( fileHighlighterType ) as IHighlighter, extensions );
            }
        }

        /// <summary>
        /// Registers a highlighter.
        /// </summary>
        /// <param name="highlighter">Highlighter instance to be registered.</param>
        /// <param name="extensions">File extensions the file highighter applies to.</param>
        public static void RegisterFileHighlighter( IHighlighter highlighter, params string[] extensions )
        {
            if( highlighter == null )
                return;

            sFileHighlighterTypes[ highlighter.GetType().Name ] = highlighter.GetType();

            foreach( var extension in extensions )
                sFileHighlighters[extension] = highlighter;
        }

        /// <summary>
        /// Unregisters a highlighter.
        /// </summary>
        /// <param name="extensions">File extensions, for which the highlighter should be unregistered.</param>
        public static void UnregisterFileHighlighter( params string[] extensions )
        {
            foreach( var extension in extensions )
            {
                IHighlighter highlighter;
                if( sFileHighlighters.TryGetValue( extension, out highlighter ) )
                    sFileHighlighterTypes.Remove( highlighter.GetType().Name );
                sFileHighlighters.Remove( extension );
            }
        }

        /// <summary>
        /// Unregisters a highlighter.
        /// </summary>
        /// <param name="highlighter">Highlighter instance, which should be unregistered.</param>
        public static void UnregisterFileHighlighter( IHighlighter highlighter )
        {
            if( highlighter == null )
                return;
            sFileHighlighterTypes.Remove( highlighter.GetType().Name );
            foreach( var extensionToUnregister in sFileHighlighters.Where( registeredFileHighlighter => registeredFileHighlighter.Value == highlighter ).Select( registeredFileHighlighter => registeredFileHighlighter.Key ).ToList() )
                sFileHighlighters.Remove( extensionToUnregister );
        }

        /// <summary>
        /// Returns a file highlighter capable of highlighting the specified file type.
        /// </summary>
        /// <param name="fileExtension">File extension, for which a matching file highlighter should be returned.</param>
        /// <returns>IHighlighter instance capable of highlighting the specified file type, or null if no was found.</returns>
        public static IHighlighter GetFileHighlighter( string fileExtension )
        {
            IHighlighter highlighter;
            if( sFileHighlighters.TryGetValue( fileExtension, out highlighter ) )
                return highlighter;
            return null;
        }

        /// <summary>
        /// Returns a file highlighter capable of highlighting the specified file.
        /// </summary>
        /// <param name="filePath">Path to the file, for which a valid highlighter should be found.</param>
        /// <returns>IHighlighter instance capable of highlighting the specified file, or null if no was found.</returns>
        public static IHighlighter GetFileHighlighterForPath( string filePath )
        {
            string extension = System.IO.Path.GetExtension( filePath );
            IHighlighter matchingHighlighter = null; 
            sFileHighlighters.TryGetValue( extension, out matchingHighlighter );            
            return matchingHighlighter;
        }
    }
}
