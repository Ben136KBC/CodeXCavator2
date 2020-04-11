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
using System.Reflection;
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
using CodeXCavator.Indexer.ViewModel;

namespace CodeXCavator.Indexer
{

    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitializeDataContextWithViewModel();
            HideIfInSilentMode();
        }

        /// <summary>
        /// Hides the main window, if app is running in silent mode.
        /// </summary>
        private void HideIfInSilentMode()
        {
            App app = Application.Current as App;
            if( app != null && app.Silent )
                this.Hide();
        }

        /// <summary>
        /// Initializes the view model and the data context.
        /// </summary>
        private void InitializeDataContextWithViewModel()
        {
            var appViewModel = new ApplicationViewModel();
            appViewModel.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler( appViewModel_PropertyChanged );
            DataContext = appViewModel;
        }

        /// <summary>
        /// Handles property changes of application view model.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        void appViewModel_PropertyChanged( object sender, System.ComponentModel.PropertyChangedEventArgs e )
        {
            if( e.PropertyName.Equals( "IsFinished" ) )
                OnIsFinishedChanged();
        }

        private void OnIsFinishedChanged()
        {
            var appViewModel = DataContext as ApplicationViewModel;
            if( appViewModel != null )
            {
                if( appViewModel.IsFinished )
                {
                    var app = Application.Current as App;
                    if( app != null && app.ShouldCloseWhenIndexingFinished )
                    {
                        app.Shutdown();
                    }
                }
            }
        }

        /// <summary>
        /// Handles the Closing event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
        {
            // Prevent the user from closing the appliction as long as indexing is still in progress.
            if( ( (ApplicationViewModel) DataContext ).IsIndexing )
            {
                MessageBox.Show( "You cannot close the indexer while indexing is in progress!", "CodeXCavator - Indexer...", MessageBoxButton.OK, MessageBoxImage.Exclamation );
                e.Cancel = true;
            }
        }

        /// <summary>
        /// Handles clicking on the info button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void imgInfo_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            ToolTip toolTip = imgInfo.ToolTip as ToolTip;
            toolTip.IsOpen = !toolTip.IsOpen;
        }

        /// <summary>
        /// Handles leaving the info button with the mouse cursor.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void imgInfo_MouseLeave( object sender, MouseEventArgs e )
        {
            ToolTip toolTip = imgInfo.ToolTip as ToolTip;
            toolTip.IsOpen = false;
        }
    }
}
