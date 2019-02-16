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
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;

namespace CodeXCavator.UI
{
    /// <summary>
    /// RichtTextBox subclass.
    /// 
    /// This RichTextBox subclass supports binding of a FlowDocument to a Text dependency property.
    /// </summary>
    public class BindableRichTextBox : RichTextBox
    {
        /// <summary>
        /// Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register( "Text", typeof( FlowDocument ), typeof( BindableRichTextBox ), new PropertyMetadata( null, OnTextPropertyChanged ) );

        /// <summary>
        /// Handles change of the Text dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnTextPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var richtTextBox = sender as RichTextBox;
            if( richtTextBox != null )
            {
                var document = eventArgs.NewValue as FlowDocument;
                richtTextBox.Document = document ?? new FlowDocument();
            }
        }

        /// <summary>
        /// C# Test dependency property wrapper.
        /// </summary>
        public FlowDocument Text
        {
            get
            {
                return GetValue( TextProperty ) as FlowDocument;
            }
            set
            {
                SetValue( TextProperty, value );
            }
        }

    }
}
