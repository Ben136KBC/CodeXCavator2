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
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Controls;
using System.Windows;

namespace CodeXCavator.UI
{
    /// <summary>
    /// FileNameToImageSourceConverter class.
    /// 
    /// The class FileNameToImageSourceConverter implements the IValueConverter interface,
    /// and converts a file path to an image source depending on the file extension.
    /// </summary>
    [ValueConversion( typeof(string), typeof(ImageSource) )]
    public class FileNameToImageSourceConverter : IValueConverter
    {
        static readonly ImageSource sImgDefault = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgCSharp = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_white_csharp.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgCPlusPlus = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_white_cplusplus.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgVisualBasic = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_white_visualstudio.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgText = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_white_text.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgConfiguration = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_white_gear.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgJava = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_white_cup.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgHtml = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_white_world.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgXml = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/page_code.png", UriKind.RelativeOrAbsolute ) );
        static readonly ImageSource sImgScript = new BitmapImage( new Uri( "/CodeXCavator.UI;component/Images/script.png", UriKind.RelativeOrAbsolute ) );
        static readonly Dictionary<string, ImageSource> sExtensionToImageMapping = new Dictionary<string, ImageSource>( StringComparer.OrdinalIgnoreCase );

        /// <summary>
        /// Initializes the file extension to image source mapping.
        /// </summary>
        static FileNameToImageSourceConverter()
        {
            sExtensionToImageMapping[".dsw"] = sImgConfiguration;
            sExtensionToImageMapping[".dsp"] = sImgConfiguration;
            sExtensionToImageMapping[".vbp"] = sImgVisualBasic;
            sExtensionToImageMapping[".sln"] = sImgConfiguration;
            sExtensionToImageMapping[".csproj"] = sImgCSharp;
            sExtensionToImageMapping[".vcproj"] = sImgCPlusPlus;
            sExtensionToImageMapping[".vcxproj"] = sImgCPlusPlus;
            sExtensionToImageMapping[".java"] = sImgJava;
            sExtensionToImageMapping[".manifest"] = sImgConfiguration;
            sExtensionToImageMapping[".properties"] = sImgConfiguration;
            sExtensionToImageMapping[".settings"] = sImgConfiguration;
            sExtensionToImageMapping[".config"] = sImgConfiguration;
            sExtensionToImageMapping[".txt"] = sImgText;
            sExtensionToImageMapping[".vb"] = sImgVisualBasic;
            sExtensionToImageMapping[".bas"] = sImgVisualBasic;
            sExtensionToImageMapping[".ctl"] = sImgVisualBasic;
            sExtensionToImageMapping[".frm"] = sImgVisualBasic;
            sExtensionToImageMapping[".cs"] = sImgCSharp;
            sExtensionToImageMapping[".xaml"] = sImgCSharp;
            sExtensionToImageMapping[".h"] = sImgCPlusPlus;
            sExtensionToImageMapping[".hxx"] = sImgCPlusPlus;
            sExtensionToImageMapping[".hpp"] = sImgCPlusPlus;
            sExtensionToImageMapping[".cpp"] = sImgCPlusPlus;
            sExtensionToImageMapping[".c"] = sImgCPlusPlus;
            sExtensionToImageMapping[".inl"] = sImgCPlusPlus;
            sExtensionToImageMapping[".htm"] = sImgHtml;
            sExtensionToImageMapping[".html"] = sImgHtml;
            sExtensionToImageMapping[".xml"] = sImgXml;
            sExtensionToImageMapping[".bat"] = sImgScript;
            sExtensionToImageMapping[".cmd"] = sImgScript;
            sExtensionToImageMapping[".vbs"] = sImgScript;
        }

        /// <summary>
        /// Converts the file path to an image file source.
        /// </summary>
        /// <param name="value">Original value. File path in this case.</param>
        /// <param name="targetType">Target type, to which the value should be converted.</param>
        /// <param name="parameter">Converter parameter.</param>
        /// <param name="culture">Culture to be used for conversion.</param>
        /// <returns>Image source for the corresponding file.</returns>
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            // Lookup the image source in the file extension map.
            ImageSource matchingImageSource;
            if( sExtensionToImageMapping.TryGetValue( System.IO.Path.GetExtension( (string) value ), out matchingImageSource ) )
                return matchingImageSource;
            // Return default image, otherwise...
            return sImgDefault;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// ItemTypeToVisibilityConverter class.
    /// 
    /// This class converts the type of the item to a visibility state.
    /// 
    /// This is used in order to hide the delete row buttons in the directory filter data grid.
    /// </summary>
    [ValueConversion( typeof(object), typeof(Visibility) )]
    public class ItemTypeToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Converts the value to a visibility state depending on the type of the value.
        /// </summary>
        /// <param name="value">Value, which should be converted. In this case this is an data grid row item.</param>
        /// <param name="targetType">Target type, to which the value should be converted.</param>
        /// <param name="parameter">Converter parameter.</param>
        /// <param name="culture">Culture to be used for conversion.</param>
        /// <returns>Visibility state depending on item type.</returns>
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            // Only show buttons for items of type DirectoryFilterEntryViewModel
            if( value is ViewModel.DirectoryFilterEntryViewModel )
                return Visibility.Visible;
            // The "new row" data grid item has an other type. Hide the buttons in this case.
            return Visibility.Hidden;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Converts an enumerable to visibility
    /// 
    /// This converter, converts a null or empty enumerable to Visible.Collapsed, and a non empty enumerable to Visibility.Visible.    
    /// </summary>
    [ValueConversion( typeof(System.Collections.IEnumerable), typeof(Visibility) )]
    public class EnumerableToVisibilityConverter : IValueConverter
    {

        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var enumerable = value as System.Collections.IEnumerable;
            if( enumerable != null )
                return enumerable.Cast<object>().Any() ? Visibility.Visible : Visibility.Collapsed;
            return Visibility.Visible;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a string to visibility.
    /// 
    /// This converter, converts a null or empty string to Visibility.Collapsed, and a non-null string to Visibility.Visible.
    /// </summary>
    [ValueConversion( typeof(string), typeof(Visibility) )]
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var stringValue = value as string;
            return !string.IsNullOrEmpty( stringValue ) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an occurrente to an occurrence view model and back.
    /// </summary>
    public class OccurrenceToOccurrenceViewModelConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var occurrenceViewModel = value as ViewModel.OccurrenceViewModel;
            if( occurrenceViewModel != null )
                return occurrenceViewModel.Occurrence;
            return null;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var occurrence = value as Engine.Interfaces.IOccurrence;
            if( occurrence != null )
                return new ViewModel.OccurrenceViewModel( occurrence, null );
            return null;
        }
    }

    /// <summary>
    /// Converts an IOccurrence enumeration to a occurrence range enumeration.
    /// </summary>
    public class OccurrenceToOccurrenceRangeConverter : IValueConverter
    {
        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var occurrences = value as IEnumerable<Engine.Interfaces.IOccurrence>;
            if( occurrences != null )
            {
                return occurrences.Select( occurrence => new Tuple<int, int, int, int>( occurrence.Column, occurrence.Line, occurrence.Column + occurrence.Match.Length, occurrence.Line ) );
            }
            return null;
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Occurrenge merge converter.
    /// 
    /// This multi value converter takes two OccurrenceViewModel or IOccurrence enumerations and merges them together into a single ordered enumeration.
    /// </summary>
    public class OccurrenceMergeConverter : IMultiValueConverter
    {
        public object Convert( object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            IEnumerable<ViewModel.OccurrenceViewModel> occurrences = null;

            foreach( var value in values )
            {
                if( occurrences == null )
                {
                    var occurrenceViewModelEnumeration = value as IEnumerable<ViewModel.OccurrenceViewModel>;
                    var occurrenceEnumeration = value as IEnumerable<Engine.Interfaces.IOccurrence>;
                    if( occurrenceViewModelEnumeration != null )
                        occurrences = occurrenceViewModelEnumeration;
                    else
                    if( occurrenceEnumeration != null )
                        occurrences = occurrenceEnumeration.Select( occurrence => new ViewModel.OccurrenceViewModel( occurrence, null ) );
                }
                else
                {
                    var occurrenceViewModelEnumeration = value as IEnumerable<ViewModel.OccurrenceViewModel>;
                    var occurrenceEnumeration = value as IEnumerable<Engine.Interfaces.IOccurrence>;
                    if( occurrenceViewModelEnumeration != null )
                        occurrences = occurrences.Concat( occurrenceViewModelEnumeration );
                    else
                        if( occurrenceEnumeration != null )
                            occurrences = occurrences.Concat( occurrenceEnumeration.Select( occurrence => new ViewModel.OccurrenceViewModel( occurrence, null ) ) );
                }
            }
            return occurrences != null ? occurrences.OrderBy( occurrenceViewModel => occurrenceViewModel.Line ) : null;
        }

        public object[] ConvertBack( object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture )
        {
            throw new NotImplementedException();
        }
    }

}
