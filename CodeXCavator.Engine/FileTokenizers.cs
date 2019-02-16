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

namespace CodeXCavator.Engine
{
    /// <summary>
    /// FileTokenizers class.
    /// 
    /// The FileTokenizers class is a global registry for file tokenizers.
    /// 
    /// It allows to register a file tokenizer for a certain file type and allows to retrieve
    /// a tokenizer capable of processing a certain file type.
    /// </summary>
    public static class FileTokenizers
    {
        private static Dictionary<string, ITokenizer> sFileTokenizers = new Dictionary<string, ITokenizer>( StringComparer.OrdinalIgnoreCase );

        static FileTokenizers()
        {
            RegisterDefaultFileTokenizers();
        }

        private static void RegisterDefaultFileTokenizers()
        {
            PluginManager.LoadPlugins<Interfaces.ITokenizer>();
        }

        /// <summary>
        /// Registers a file tokenizer.
        /// </summary>
        /// <typeparam name="FILE_TOKENIZER_TYPE">Type of the file tokenzier to be registered.</typeparam>
        /// <param name="extensions">File extensions of files to which the tokenizer can be applied.</param>
        public static void RegisterFileTokenizer<FILE_TOKENIZER_TYPE>( params string[] extensions ) where FILE_TOKENIZER_TYPE : ITokenizer, new()
        {
            RegisterFileTokenizer( new FILE_TOKENIZER_TYPE(), extensions );
        }
        
        /// <summary>
        /// Registers a file tokenizer.
        /// </summary>
        /// <param name="fileTokenizerType">Type of the file tokenzier to be registered.</param>
        /// <param name="extensions">File extensions of files to which the tokenizer can be applied.</param>
        public static void RegisterFileTokenizer( Type fileTokenizerType, params string[] extensions )
        {
            if( fileTokenizerType != null && typeof( ITokenizer ).IsAssignableFrom( fileTokenizerType ) )
            {
                var fileExtensionsAttribute = fileTokenizerType.GetCustomAttributes(true).Where( attribute => typeof( Attributes.FileExtensionsAttribute ).IsAssignableFrom( attribute.GetType() ) ).Cast< Attributes.FileExtensionsAttribute >().FirstOrDefault();
                if( fileExtensionsAttribute != null && extensions.Length == 0 )
                    RegisterFileTokenizer( Activator.CreateInstance( fileTokenizerType ) as ITokenizer, fileExtensionsAttribute.FileExtensions );
                else
                    RegisterFileTokenizer( Activator.CreateInstance( fileTokenizerType ) as ITokenizer, extensions );
            }
        }

        /// <summary>
        /// Registers a file tokenizer.
        /// </summary>
        /// <param name="tokenizer">Instace of tokenizer, which should be registered.</param>
        /// <param name="extensions">File extensions of files to which the tokenizer can be applied.</param>
        public static void RegisterFileTokenizer( ITokenizer tokenizer, params string[] extensions )
        {
            if( tokenizer == null )
                return;

            foreach( var extension in extensions )
                sFileTokenizers[extension] = tokenizer;
        }

        /// <summary>
        /// Unregisters a file tokenizer.
        /// </summary>
        /// <param name="extensions">List of file extensions, for which the tokenizer should be unregistered.</param>
        public static void UnregisterFileTokenizer( params string[] extensions )
        {
            foreach( var extension in extensions )
                sFileTokenizers.Remove( extension );
        }

        /// <summary>
        /// Unregisters a file tokenizer.
        /// </summary>
        /// <param name="fileTokenizer">File tokenizer, which should be unregistered.</param>
        public static void UnregisterFileTokenizer( ITokenizer fileTokenizer )
        {
            foreach( var extensionToUnregister in sFileTokenizers.Where( registeredFileTokenizer => registeredFileTokenizer.Value == fileTokenizer ).Select( registeredFileTokenizer => registeredFileTokenizer.Key ).ToList() )
                sFileTokenizers.Remove( extensionToUnregister );
        }

        /// <summary>
        /// Returns a file tokenizer capable of processing the specified file type.
        /// </summary>
        /// <param name="fileExtension">File extensions for which a matching file tokenizer should be found.</param>
        /// <returns>ITokenizer instance capable of processing the specified file type, or null if no matching file tokenizer was found.</returns>
        public static ITokenizer GetFileTokenizer( string fileExtension )
        {
            ITokenizer tokenizer;
            if( sFileTokenizers.TryGetValue( fileExtension, out tokenizer ) )
                return tokenizer;
            return null;
        }

        /// <summary>
        /// Returns a file tokenizer capable of processing the specified file.
        /// </summary>
        /// <param name="filePath">Path of the file, for which a matching file tokenizer should be found.</param>
        /// <returns>ITokenizer instance capable of processing the specified file, or null if no matching file tokenizer was found.</returns>
        public static ITokenizer GetFileTokenizerForPath( string filePath )
        {
            string extension = System.IO.Path.GetExtension( filePath );
            ITokenizer matchingTokenizer;
            sFileTokenizers.TryGetValue( extension, out matchingTokenizer );
            return matchingTokenizer;
        }
    }
}
