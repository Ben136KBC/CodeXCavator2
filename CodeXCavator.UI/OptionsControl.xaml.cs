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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using CodeXCavator.Engine.Interfaces;
using System.ComponentModel;
using System.Windows.Markup;
using System.Windows.Controls.Primitives;

namespace CodeXCavator.UI
{
    /// <summary>
    /// Interaktionslogik für OptionsControl.xaml
    /// </summary>
    public partial class OptionsControl : UserControl
    {

        public static readonly DependencyProperty OptionsProviderProperty = DependencyProperty.Register( "OptionsProvider", typeof( IOptionsProvider ), typeof( OptionsControl ), new UIPropertyMetadata( null, OnOptionsProviderPropertyChanged ) );

        private static void OnOptionsProviderPropertyChanged( object sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var optionsControl = sender as OptionsControl;
            if( optionsControl != null )
                optionsControl.OnOptionsProviderChanged( eventArgs.OldValue as IOptionsProvider, eventArgs.NewValue as IOptionsProvider );
        }

        protected virtual void OnOptionsProviderChanged( IOptionsProvider previousOptionsProvider, IOptionsProvider currentOptionsProvider )
        {            
            ((FrameworkElement) this.Content).DataContext = currentOptionsProvider != null ? currentOptionsProvider.Options.Select( option => new ViewModel.OptionViewModel( option, currentOptionsProvider ) ) : null; 
        }

        public IOptionsProvider OptionsProvider
        {
            get { return (IOptionsProvider) GetValue( OptionsProviderProperty ); }
            set { SetValue( OptionsProviderProperty, value ); }
        }
                      
        public OptionsControl()
        {
            InitializeComponent();
        }

        private void Options_ToggleButton_Checked( object sender, RoutedEventArgs e )
        {
            SetToggleButtonOption( sender as ToggleButton);
        }

        private void Options_ToggleButton_Unchecked( object sender, RoutedEventArgs e )
        {
            SetToggleButtonOption( sender as ToggleButton);
        }

        private void SetToggleButtonOption( ToggleButton toggleButton )
        {
            if( OptionsProvider == null )
                return;
            
            if( toggleButton == null )
                return;

            var option = toggleButton.DataContext as IOption;
            if( option == null )
                return;

            OptionsProvider.SetOptionValue( option, toggleButton.IsChecked );
        }

    }

    public class TypeTemplate : DependencyObject
    {
        public Type Type { get; set; }
        public DataTemplate Template { get; set; }
    }

    [ContentProperty("TypeTemplates")]
    public class OptionTypeTemplateSelector : DataTemplateSelector
    {
        public List<TypeTemplate> TypeTemplates { get; private set; }

        public OptionTypeTemplateSelector()
        {
            TypeTemplates = new List<TypeTemplate>();
        }

        public override DataTemplate SelectTemplate( object item, DependencyObject container )
        {
            if( item == null )
                return null;

            var itemAsOption = item as IOption;
            var itemType = itemAsOption != null ? itemAsOption.ValueType : item.GetType();

            foreach( var typeTemplate in TypeTemplates )
            {
                if( typeTemplate.Type.IsAssignableFrom( itemType ) )
                    return typeTemplate.Template;
            }

            foreach( var typeTemplate in TypeTemplates )
            {
                if( typeTemplate.Type == null )
                    return typeTemplate.Template;
            }

            return null;
        }
    }
}
