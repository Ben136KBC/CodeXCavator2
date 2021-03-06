﻿// Copyright 2014 Christoph Brzozowski
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
using System.IO.IsolatedStorage;
using System.IO;
using CodeXCavator.Engine.Interfaces;

//This file has been significantly modified by Ben van der Merwe
//
//CodeXCavator has been modified by Ben van der Merwe and enhanced as follows:
//- Qeury text box gets keyboard focus when the application opens.
//- Main window location and size gets restored upon open.
//- Search is case insensitive by default.
//- View to select a recently used index or create a new simple index.
//- Button to update the existing index
//- Button to reopen current index by default on next startup.
//- Etc.

namespace CodeXCavator
{
    /// <summary>
    /// MainWindow class.
    /// 
    /// This class is actually only a host window for the SearchControl control from CodeXCavator.UI.
    /// </summary>
    public partial class MainWindow : Window
    {
        string windowLocationFilename = "location.txt";

        public MainWindow()
        {
            InitializeComponent();
            CodeXCavator.UI.MRUHandler.OpenCreateIndexViewMethod += new EventHandler(OpenCreateIndexView_method);
            // Read and initialize user settings
            var userSettings = new UI.UserSettings();
            new Engine.RegistryUserSettingsStorageProvider().Restore( "CodeXCavator", userSettings );
            srcSearcher.UserSettings = userSettings;
            // Initialize the search control with the list of available indexes. 
            IEnumerable<IIndex> indexes = ((App)Application.Current).Indexes;
            srcSearcher.Indexes = indexes;

            //If there are no indexes on the command line argument, then reopen the last one,
            //if the user has checked that option.
            if (userSettings.ReOpenLastFile!=null && userSettings.ReOpenLastFile == "true" &&
                userSettings.LastSelectedIndex!=null && userSettings.LastSelectedIndex!="")
            {
                string path = userSettings.LastSelectedIndex;
                string indexFile = CodeXCavator.UI.MRUHandler.GetFileWithPath(path);
                if (OpenIndexFile(indexFile))
                {
                    indexes = srcSearcher.Indexes;
                    srcSearcher.SetReOpenNextTimeCheckBox();
                }
                else
                {
                    userSettings.ReOpenLastFile = "false";
                    MessageBox.Show(string.Format("Could not reopen previous index file for \"{0}\"!", path), "CodeXCavator - Searcher...", MessageBoxButton.OK, MessageBoxImage.Information);
                    new Engine.RegistryUserSettingsStorageProvider().Store("CodeXCavator", srcSearcher.UserSettings);
                }
            }

            // If there are no indices, open up the select or create new index view.
            if (!indexes.Any())
            {
                SelectCreateIndexWindow selectCreateIndexWindow = new SelectCreateIndexWindow();
                selectCreateIndexWindow.ShowDialog();
                //The above window may have changed entries, so refresh our settings.
                userSettings = new UI.UserSettings();
                new Engine.RegistryUserSettingsStorageProvider().Restore("CodeXCavator", userSettings);
                srcSearcher.UserSettings = userSettings;

                string indexFile = selectCreateIndexWindow.IndexFileToOpen;
                if (OpenIndexFile(indexFile))
                {
                    indexes = srcSearcher.Indexes;
                }
                else if (indexFile!=null && indexFile!="")
                {
                    MessageBox.Show(string.Format("Could not initialize index from configuration file \"{0}\"!", indexFile), "CodeXCavator - Searcher...", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            // Still no indices? Then exit.
            if (!indexes.Any())
            {
                Application.Current.Shutdown();
            }

            // Refresh restore bounds from previous window opening
            IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
            try
            {
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(windowLocationFilename, FileMode.Open, storage))
                using (StreamReader reader = new StreamReader(stream))
                {

                    // Read restore bounds value from file
                    Rect restoreBounds = Rect.Parse(reader.ReadLine());
                    this.Left = restoreBounds.Left;
                    this.Top = restoreBounds.Top;
                    this.Width = restoreBounds.Width;
                    this.Height = restoreBounds.Height;
                }
            }
            catch (FileNotFoundException /*ex*/)
            {
                // Handle when file is not found in isolated storage, which is when:
                // * This is first application session
                // * The file has been deleted
            }
        }

        private void Window_Closed( object sender, EventArgs e )
        {
            // Store user settings
            new Engine.RegistryUserSettingsStorageProvider().Store( "CodeXCavator", srcSearcher.UserSettings );
        }

        void Window_Closing(object sender, EventArgs e)
        {
            try
            {
                // Save restore bounds for the next time this window is opened
                IsolatedStorageFile storage = IsolatedStorageFile.GetUserStoreForAssembly();
                using (IsolatedStorageFileStream stream = new IsolatedStorageFileStream(windowLocationFilename, FileMode.Create, storage))
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    // Write restore bounds value to file
                    writer.WriteLine(this.RestoreBounds.ToString());
                }
            }
            catch { }
        }

        /// <summary>
        /// Open an index configuraation file by name.
        /// </summary>
        private bool OpenIndexFile(string indexFile)
        {
            if (indexFile != null && indexFile != "")
            {
                //Try and add this as an index:
                System.IO.FileInfo fInfo = new System.IO.FileInfo(indexFile);
                if (fInfo.Exists)
                {
                    IIndex index = IIndexExtensions.CreateFromXmlFile(indexFile);
                    if (index != null)
                    {
                        // Add index to list of indexes.
                        var newIndixes = new List<IIndex>();
                        newIndixes.Add(index);
                        srcSearcher.Indexes = newIndixes;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// While the main window is open, show the Index open or create view.
        /// That allows the user to select an index or create a new one.
        /// </summary>
        protected void OpenCreateIndexView_method(object sender, EventArgs e)
        {
            SelectCreateIndexWindow selectCreateIndexWindow = new SelectCreateIndexWindow();
            selectCreateIndexWindow.ShowDialog();
            string indexFile = selectCreateIndexWindow.IndexFileToOpen;
            if (indexFile == null || indexFile == "")
            {
                return;
            }
            try
            {
                System.IO.FileInfo fInfo = new System.IO.FileInfo(indexFile);
                if (!fInfo.Exists)
                {
                    MessageBox.Show(string.Format("Could not find file \"{0}\"!", indexFile), "CodeXCavator - Searcher...", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
            }
            catch
            {
            }
            if (OpenIndexFile(indexFile))
            {
                //Success!
            }
        }
    }
}
