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
using System.Windows.Media.Imaging;

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// OptionViewModel class.
    /// 
    /// This class implements a view model for the IOption interface.
    /// </summary>
    public class OptionViewModel : ViewModelBase, IOption
    {
        private IOption mOption;
        private IOptionsProvider mOptionsProvider;
        private ImageSource mIcon;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="option">Option, which should be encapsulated.</param>
        /// <param name="optionsProvider">Options provider, which should be used for manipulating the option value.</param>
        public OptionViewModel( IOption option, IOptionsProvider optionsProvider )
        {
            mOption = option;
            mOptionsProvider = optionsProvider;
            mIcon = LoadIcon( option );
        }

        /// <summary>
        /// Tries to load the icon for the given option.
        /// </summary>
        /// <param name="option">Option, whose icon should be loaded.</param>
        /// <returns>Image source of the loaded icon, or null, if no icon was provided, or icon could not have been loaded.</returns>
        private static ImageSource LoadIcon( IOption option )
        {
            var iconProvider = option as IIconProvider;
            if( iconProvider == null )
                return null;

            var icon = iconProvider.Icon;
            
            var imageSource = icon as ImageSource;
            if( imageSource != null )
                return imageSource;
            
            var imageUri = icon as string;
            if( imageUri != null )
            {
                try
                {
                    var iconImage = new BitmapImage( new Uri( imageUri, UriKind.RelativeOrAbsolute ) );
                    return iconImage;
                }
                catch
                {                    
                }
            }

            return null;
        }

        /// <summary>
        /// Name of the option.
        /// </summary>
        public string Name
        {
            get
            {
                return mOption != null ? mOption.Name : null;
            }
        }

        /// <summary>
        /// Value type of the option.
        /// </summary>
        public Type ValueType
        {
            get { return mOption != null ? mOption.ValueType : null; }
        }

        /// <summary>
        /// Caption of the option, if available.
        /// </summary>
        public string Caption
        {
            get
            {
                var captionProvider = mOption as ICaptionProvider;
                if( captionProvider != null )
                    return captionProvider.Caption;
                return null;
            }
        }

        /// <summary>
        /// Description of the option, if available.
        /// </summary>
        public string Description
        {
            get
            {
                var descriptionProvider = mOption as IDescriptionProvider;
                if( descriptionProvider != null )
                    return descriptionProvider.Description;
                return null;
            }
        }

        /// <summary>
        /// Icon of the option.
        /// </summary>
        public ImageSource Icon
        {
            get { return mIcon; }
        }

        /// <summary>
        /// Value of the option, if available.
        /// 
        /// This can be used to modify the option value.
        /// </summary>
        public object Value
        {
            get
            {
                if( mOption != null && mOptionsProvider != null )
                {
                    return mOptionsProvider.GetOptionValue( mOption );
                }
                return null;
            }
            set
            {
                if( mOption != null && mOptionsProvider != null )
                {
                    mOptionsProvider.SetOptionValue( mOption, value );
                }
            }
        }

        /// <summary>
        /// Determines, whether the icon should be shown.
        /// </summary>
        public bool IsIconVisible
        {
            get { return mIcon != null; }
        }

        /// <summary>
        /// Determines, whether the caption should be shown.
        /// </summary>
        public bool IsCaptionVisible
        {
            get { return !IsIconVisible; }
        }
    }
}
