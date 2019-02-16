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

namespace CodeXCavator.Engine.Tokenizers
{
    /// <summary>
    /// Token class.
    /// 
    /// Generic implementation of IToken interface, which can be returned by tokenizers.
    /// </summary>
    public class Token : IToken
    {
        /// <summary>
        /// INVALID constant, either indicating an invalid position, offset or length.
        /// </summary>
        public const int INVALID = -1;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Token() : this( null, null, INVALID, INVALID, INVALID, INVALID )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        /// <param name="length">Token length given in characters.</param>
        public Token( string text, string type, int position, int length ) : this( text, type, position, length, INVALID, INVALID )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="type">Token data.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        /// <param name="length">Token length given in characters.</param>
        public Token( string text, string type, object data, int position, int length ) : this( text, type, data, position, length, INVALID, INVALID )
        {
        }


        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        public Token( string text, string type, int position ) : this( text, type, position, text.Length, INVALID, INVALID )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="data">Token data.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        public Token( string text, string type, object data, int position ) : this( text, type, data, position, text.Length, INVALID, INVALID )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        /// <param name="line">Zero based line number, of the token's location.</param>
        /// <param name="column">Zero based column number, of the token's location.</param>
        public Token( string text, string type, int position, int line, int column ) : this( text, type, position, text.Length, line, column )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        /// <param name="line">Zero based line number, of the token's location.</param>
        /// <param name="column">Zero based column number, of the token's location.</param>
        public Token( string text, string type, object data, int position, int line, int column ) : this( text, type, data, position, text.Length, line, column )
        {
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        /// <param name="length">Token length.</param>
        /// <param name="line">Zero based line number, of the token's location.</param>
        /// <param name="column">Zero based column number, of the token's location.</param>
        public Token( string text, string type, int position, int length, int line, int column )
        {
            Text = text;
            Type = type;
            Position = position;
            Length = length;
            Line = line;
            Column = column;
        }

        /// <summary>
        /// Initialization constructor.
        /// </summary>
        /// <param name="text">Token text.</param>
        /// <param name="type">Token type.</param>
        /// <param name="type">Token Data.</param>
        /// <param name="position">Absolute position of the token within the file.</param>
        /// <param name="length">Token length.</param>
        /// <param name="line">Zero based line number, of the token's location.</param>
        /// <param name="column">Zero based column number, of the token's location.</param>
        public Token( string text, string type, object data, int position, int length, int line, int column )
        {
            Text = text;
            Type = type;
            Position = position;
            Length = length;
            Line = line;
            Column = column;
            Data = data;
        }

        public string Type
        {
            get;
            set;
        }

        public string Text
        {
            get;
            set;
        }

        public int Position
        {
            get;
            set;
        }

        public int Length
        {
            get;
            set;
        }

        public int Line
        {
            get;
            set;
        }

        public int Column
        {
            get;
            set;
        }

        public object Data
        {
            get;
            set;
        }
    }
}
