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

namespace CodeXCavator.UI.ViewModel
{
    /// <summary>
    /// View model class for ITextSearcher instances.
    /// </summary>
    internal class TextSearcherViewModel : ViewModelBase
    {
        private ITextSearcher mTextSearcher;
        private IDescriptionProvider mTextSearcherAsDescriptionProvider;
        private IIconProvider mTextSearcherAsIconProvider;
        private ICaptionProvider mTextSearcherAsCaptionProvider;

        public override bool Equals( object obj )
        {
            if( ReferenceEquals( this, obj ) )
                return true;
            var textSearcher = obj as ITextSearcher;
            if( ReferenceEquals( this.mTextSearcher, textSearcher ) && obj != null )
                return true;
            var textSearcherViewModel = obj as TextSearcherViewModel;
            if( textSearcherViewModel != null )
                return ReferenceEquals( this.mTextSearcher, textSearcherViewModel.mTextSearcher );
            return false;
        }

        public override int GetHashCode()
        {
            if( mTextSearcher == null )
                return 0;
            return this.mTextSearcher.GetHashCode();
        }

        internal TextSearcherViewModel( ITextSearcher textSearcher )
        {
            mTextSearcher = textSearcher;
            mTextSearcherAsCaptionProvider = textSearcher as ICaptionProvider;
            mTextSearcherAsDescriptionProvider = textSearcher as IDescriptionProvider;
            mTextSearcherAsIconProvider = textSearcher as IIconProvider;
        }

        /// <summary>
        /// Underlying text searcher.
        /// </summary>
        public ITextSearcher TextSearcher
        {
            get { return mTextSearcher; }
        }

        /// <summary>
        /// Caption or display label for the text searcher.
        /// 
        /// Will be used as caption of the button.
        /// </summary>
        public string Caption
        {
            get { return mTextSearcherAsCaptionProvider != null ? mTextSearcherAsCaptionProvider.Caption : mTextSearcher.GetType().Name; }
        }

        /// <summary>
        /// Description of the text searcher.
        /// 
        /// Will be used as a tooltip.
        /// </summary>
        public string Description
        {
            get { return mTextSearcherAsDescriptionProvider != null ? mTextSearcherAsDescriptionProvider.Description : null; }
        }

        /// <summary>
        /// Icon for the text searcher.
        /// 
        /// Will be used as 
        /// </summary>
        public object Icon
        {
            get { return mTextSearcherAsIconProvider != null ? mTextSearcherAsIconProvider.Icon : null; }
        }


        /// <summary>
        /// Determines, whether the caption should be displayed.
        /// </summary>
        public bool IsCaptionVisible
        {
            get { return Icon == null; } 
        }

        /// <summary>
        /// Determines, whether the icon should be displayed.
        /// </summary>
        public bool IsIconVisible
        {
            get { return Icon != null; }
        }
    }
}
