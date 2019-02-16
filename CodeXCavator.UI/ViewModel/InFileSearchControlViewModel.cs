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
using System.Windows.Media;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// InFileSearchControlViewModel class.
    /// 
    /// View model for the InFileSearchControl.
    /// </summary>
    internal class InFileSearchControlViewModel : ViewModelBase
    {
        /// <summary>
        /// Text, which should be searched.
        /// </summary>
        private string mText;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="text">Text, which should be searched.</param>
        internal InFileSearchControlViewModel( string text )
        {
            mText = text;
        }

        /// <summary>
        /// Text, which should be searched.
        /// </summary>
        public string Text
        {
            get { return mText; }
            set
            {
                if( mText != value )
                {
                    mText = value;
                    OnPropertyChanged( "Text" );
                }
            }
        }

        /// <summary>
        /// Colors for the search highlight color color picker.
        /// </summary>
        public static IEnumerable<SolidColorBrush> Colors
        {
            get
            {
                return GetColorsFromMediaBrushesClass().OrderBy( color => color, new ColorComparer() );
            }
        }

        /// <summary>
        /// Compares two colors by hue.
        /// </summary>
        private class ColorComparer : IComparer<SolidColorBrush>
        {
            public int Compare( SolidColorBrush x, SolidColorBrush y )
            {
                return (int) ( ( GetSortValue( x.Color ) - GetSortValue( y.Color ) ) * 255.0f );
            }

            private static float GetSortValue( System.Windows.Media.Color c )
            {
                var color = System.Drawing.Color.FromArgb( c.A, c.R, c.G, c.B );
                return ( color.GetHue() / 360.0f );
            }
        }

        /// <summary>
        /// Returns colors provided by the Media.Brushes class.
        /// </summary>
        /// <returns>Sorted list of color brushes.</returns>
        private static IEnumerable<SolidColorBrush> GetColorsFromMediaBrushesClass()
        {
            foreach( var brushProperty in typeof( System.Windows.Media.Brushes ).GetProperties( System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Public ) )
            {
                if( brushProperty.PropertyType == typeof( System.Windows.Media.SolidColorBrush ) )
                {
                    yield return (SolidColorBrush) brushProperty.GetValue( null, null );
                }
            }
        }
    }
}
