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
using System.Reflection;
using System.IO;

namespace CodeXCavator.Engine
{
    public static class PluginManager
    {
        private const string PLUGIN_DIRECTORY = "plugins";
        private static bool mPluginsLoadedFromDefaultPluginDirectory;
        private static List< Assembly > mLoadedPluginAssemblies = new List<Assembly>();

        /// <summary>
        /// Static constructor.
        /// </summary>
        static PluginManager()
        {
        }
        
        /// <summary>
        /// Loads plugins from default plugin directory.
        /// </summary>
        internal static void LoadPlugins()
        {
            if( mPluginsLoadedFromDefaultPluginDirectory )
                return;

            string pluginManagerAssemblyDirectory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
            string pluginDirectory = Path.Combine( pluginManagerAssemblyDirectory, PLUGIN_DIRECTORY );
            LoadPluginsFromDirectory( pluginDirectory ); 

            mPluginsLoadedFromDefaultPluginDirectory = true;
        }

        /// <summary>
        /// Loads plugins from default plugin directory.
        /// </summary>
        /// <typeparam name="PLUGIN_TYPE">Type of plugin to be loaded.</typeparam>
        internal static void LoadPlugins< PLUGIN_TYPE >()
        {
            LoadPlugins( typeof( PLUGIN_TYPE ) );
        }

        /// <summary>
        /// Loads plugins from default plugin directory.
        /// </summary>
        /// <param name="pluginType">Type of plugin to be loaded.</param>
        internal static void LoadPlugins( Type pluginType )
        {
            if( !mPluginsLoadedFromDefaultPluginDirectory )
            {
                string pluginManagerAssemblyDirectory = Path.GetDirectoryName( Assembly.GetExecutingAssembly().Location );
                string pluginDirectory = Path.Combine( pluginManagerAssemblyDirectory, PLUGIN_DIRECTORY );
                LoadPluginsFromDirectory( pluginDirectory, pluginType );
                mPluginsLoadedFromDefaultPluginDirectory = true; 
            }
            else
            {
                foreach( var pluginAssembly in mLoadedPluginAssemblies )
                    RegisterTypesFromPluginAssembly( pluginAssembly, pluginType );
            }
        }

        /// <summary>
        /// Loads plugins from the specified directory.
        /// 
        /// Only assemblies, which start with "Plugin." as their name prefix, are loaded.
        /// </summary>
        /// <param name="pluginDirectory">Directory containing plugin assemblies.</param>
        public static void LoadPluginsFromDirectory( string pluginDirectory )
        {
            if( Directory.Exists( pluginDirectory ) )
            {
                foreach( var assemblyPath in Directory.EnumerateFiles( pluginDirectory, "Plugin.*.dll", SearchOption.TopDirectoryOnly ) )
                {
                    LoadPluginAssembly( assemblyPath );
                }
            }
        }

        /// <summary>
        /// Loads plugins from the specified directory.
        /// 
        /// Only assemblies, which start with "Plugin." as their name prefix, are loaded.
        /// </summary>
        /// <param name="pluginDirectory">Directory containing plugin assemblies.</param>
        /// <typeparam name="PLUGIN_TYPE">Type of plugin to be loaded.</typeparam>
        public static void LoadPluginsFromDirectory<PLUGIN_TYPE>( string pluginDirectory )
        {
            LoadPluginsFromDirectory( pluginDirectory, typeof( PLUGIN_TYPE ) );
        }

        /// <summary>
        /// Loads plugins from the specified directory.
        /// 
        /// Only assemblies, which start with "Plugin." as their name prefix, are loaded.
        /// </summary>
        /// <param name="pluginDirectory">Directory containing plugin assemblies.</param>
        /// <param name="pluginType">Type of plugin to be loaded.</param>
        public static void LoadPluginsFromDirectory( string pluginDirectory, Type pluginType )
        {
            if( Directory.Exists( pluginDirectory ) )
            {
                foreach( var assemblyPath in Directory.EnumerateFiles( pluginDirectory, "Plugin.*.dll", SearchOption.TopDirectoryOnly ) )
                {
                    LoadPluginAssembly( assemblyPath, pluginType );
                }
            }
        }
       
        /// <summary>
        /// Loads a plugin assembly.
        /// </summary>
        /// <param name="pluginAssemblyPath">Path of the plugin assembly.</param>
        public static void LoadPluginAssembly( string pluginAssemblyPath )
        {
            if( System.IO.File.Exists( pluginAssemblyPath ) )
            {
                try
                {
                    Assembly pluginAssembly = Assembly.UnsafeLoadFrom( pluginAssemblyPath );
                    LoadPluginAssembly( pluginAssembly );
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Loads a plugin assembly.
        /// </summary>
        /// <param name="pluginAssemblyPath">Path of the plugin assembly.</param>
        /// <typeparam name="PLUGIN_TYPE">Type of plugin to be loaded.</typeparam>
        public static void LoadPluginAssembly<PLUGIN_TYPE>( string pluginAssemblyPath )
        {
            LoadPluginAssembly( pluginAssemblyPath, typeof( PLUGIN_TYPE ) );
        }

        /// <summary>
        /// Loads a plugin assembly.
        /// </summary>
        /// <param name="pluginAssemblyPath">Path of the plugin assembly.</param>
        /// <param name="pluginType">Type of plugin to be loaded.</param>
        public static void LoadPluginAssembly( string pluginAssemblyPath, Type pluginType )
        {
            if( System.IO.File.Exists( pluginAssemblyPath ) )
            {
                try
                {
                    Assembly pluginAssembly = Assembly.UnsafeLoadFrom( pluginAssemblyPath );
                    LoadPluginAssembly( pluginAssembly, pluginType );
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Loads a plugin assembly.
        /// </summary>
        /// <param name="pluginAssembly">Plugin assembly.</param>
        public static void LoadPluginAssembly( Assembly pluginAssembly )
        {
            if( pluginAssembly != null )
            {
                mLoadedPluginAssemblies.Add( pluginAssembly );
                RegisterTypesFromPluginAssembly( pluginAssembly );
            }
        }

        /// <summary>
        /// Loads a plugin assembly.
        /// </summary>
        /// <param name="pluginAssembly">Plugin assembly.</param>
            /// <typeparam name="PLUGIN_TYPE">Type of plugin to be loaded.</typeparam>
        public static void LoadPluginAssembly<PLUGIN_TYPE>( Assembly pluginAssembly )
        {
            LoadPluginAssembly( pluginAssembly, typeof( PLUGIN_TYPE ) );
        }

        /// <summary>
        /// Loads a plugin assembly.
        /// </summary>
        /// <param name="pluginAssembly">Plugin assembly.</param>
        /// <param name="pluginType">Type of plugin to be loaded.</param>
        public static void LoadPluginAssembly( Assembly pluginAssembly, Type pluginType )
        {
            if( pluginAssembly != null )
            {
                mLoadedPluginAssemblies.Add( pluginAssembly );
                RegisterTypesFromPluginAssembly( pluginAssembly, pluginType );
            }
        }

        /// <summary>
        /// Registers all or certain types from the specified plugin assembly.
        /// </summary>
        /// <param name="pluginAssembly">Plugin assembly.</param>
        /// <param name="pluginType">Type of pluign to be registered. If null, all supported types are registered.</param>
        private static void RegisterTypesFromPluginAssembly( Assembly pluginAssembly, Type pluginType = null )
        {
            foreach( var exportedType in pluginAssembly.GetExportedTypes() )
            {
                try
                {
                    if( ( pluginType == null || typeof( Interfaces.IFileAction ) == pluginType ) && typeof( Interfaces.IFileAction ).IsAssignableFrom( exportedType ) )
                        FileActions.RegisterFileAction( exportedType );
                    else
                    if( ( pluginType == null || typeof( Interfaces.IFileCatalogueEnumerator ) == pluginType ) && typeof( Interfaces.IFileCatalogueEnumerator ).IsAssignableFrom( exportedType ) )
                        FileCatalogueEnumerators.RegisterFileCatalogueEnumerator( exportedType );
                    else
                    if( ( pluginType == null || typeof( Interfaces.IFileEnumerator ) == pluginType ) && typeof( Interfaces.IFileEnumerator ).IsAssignableFrom( exportedType ) )
                        FileEnumerators.RegisterFileEnumerator( exportedType );
                    else
                    if( ( pluginType == null || typeof( Interfaces.IFileFilter ) == pluginType ) && typeof( Interfaces.IFileFilter ).IsAssignableFrom( exportedType ) )
                        FileFilters.RegisterFileFilter( exportedType );
                    else
                    if( ( pluginType == null || typeof( Interfaces.IHighlighter ) == pluginType ) && typeof( Interfaces.IHighlighter ).IsAssignableFrom( exportedType ) )
                        FileHighlighters.RegisterFileHighlighter( exportedType );
                    else
                    if( ( pluginType == null || typeof( Interfaces.ITokenizer ) == pluginType ) && typeof( Interfaces.ITokenizer ).IsAssignableFrom( exportedType ) )
                        FileTokenizers.RegisterFileTokenizer( exportedType );
                    else
                    if( ( pluginType == null || typeof( Interfaces.IFileStorageProvider ) == pluginType ) && typeof( Interfaces.IFileStorageProvider ).IsAssignableFrom( exportedType ) )
                        FileStorageProviders.RegisterFileStorageProvider( exportedType );
                    else
                    if( ( pluginType == null || typeof( Interfaces.ITextSearcher ) == pluginType ) && typeof( Interfaces.ITextSearcher ).IsAssignableFrom( exportedType ) )
                        TextSearchers.RegisterTextSearcher( exportedType );
                }
                catch
                {
                }
            }
        }
    }
}
