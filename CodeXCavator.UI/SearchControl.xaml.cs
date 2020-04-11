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

//This file has been modified by Ben van der Merwe

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CodeXCavator.Engine.Interfaces;
using System.Windows.Controls.Primitives;
using CodeXCavator.UI.ViewModel;
using System.Windows.Data;
using System.ComponentModel;

namespace CodeXCavator.UI
{
    /// <summary>
    /// SearchControl class.
    /// 
    /// The SearchControl class provides a user interface for performing search
    /// on a list of indexes. It handles searching and the display of search results.
    /// </summary>
    public partial class SearchControl : UserControl    
    {
        /// <summary>
        /// Indexes dependency property.
        /// </summary>
        public static readonly DependencyProperty IndexesProperty = DependencyProperty.Register( "Indexes", typeof( IEnumerable<IIndex> ), typeof( SearchControl ), new PropertyMetadata( null, OnIndexesChanged ) );

        /// <summary>
        /// Handles the change of the Indexes dependency property.
        /// </summary>
        /// <param name="d">Dependency object containing the property.</param>
        /// <param name="e">Property change event arguments.</param>
        private static void OnIndexesChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( (SearchControl) d ).OnIndexesChanged( e.OldValue as IEnumerable<IIndex>, e.NewValue as IEnumerable<IIndex> );
        }

        /// <summary>
        /// Handles the change of the Indexes property.
        /// </summary>
        /// <param name="oldValue">Old property value.</param>
        /// <param name="newValue">New property value.</param>
        protected virtual void OnIndexesChanged( IEnumerable<IIndex> oldValue, IEnumerable<IIndex> newValue )
        {
            DataContext = new ViewModel.SearchControlViewModel( newValue, UserSettings );
        }

        /// <summary>
        /// Sets or returns the list of indexes, on which search can be performed using the search control.
        /// </summary>
        public IEnumerable<IIndex> Indexes
        {
            get             
            {
                return GetValue( IndexesProperty ) as IEnumerable<IIndex>;
            }
            set
            {
                SetValue( IndexesProperty, value );
            }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SearchControl()
        {
            InitializeComponent();
            this.Loaded += Src_Loaded;
		}

        void Src_Loaded(object sender, RoutedEventArgs e)
        {
            SearchQueryTextBox.Focus();
        }

        /// <summary>
        /// Finds the ancestor of the given type of the specified object.
        /// </summary>
        /// <typeparam name="ANCESTOR_TYPE">Type of the ancestor, which should be searched.</typeparam>
        /// <param name="o">Dependency object, for which the ancestor of the specified type should be searched.</param>
        /// <returns>Ancestor of o with the given type, or null, if no such ancestor could have been found.</returns>
        private static ANCESTOR_TYPE FindAncestor<ANCESTOR_TYPE>( DependencyObject o ) where ANCESTOR_TYPE : DependencyObject
        {
            DependencyObject current = o;
            while( current != null && !(current is ANCESTOR_TYPE) )
            {
                if( current is Visual )
                    current = VisualTreeHelper.GetParent( current );
                else
                    current = LogicalTreeHelper.GetParent( current );
            }
            return current as ANCESTOR_TYPE;
        }

        /// <summary>
        /// Handles a click on the CloseDocument button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CloseDocumentButton_Click( object sender, RoutedEventArgs e )
        {
            Button closeButton = sender as Button;
            // Close the document the button belongs to
            ViewModel.SearchControlViewModel viewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            var documentViewModel = closeButton.DataContext as ViewModel.DocumentViewModel;
            viewModel.CloseDocument( documentViewModel.FilePath );            
        }

        /// <summary>
        /// Handles a click on the SearchInDocument button
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void SearchInDocumentButton_Click( object sender, RoutedEventArgs e )
        {
            var frameworkElement = sender as FrameworkElement;
            if( frameworkElement == null )
                return;
            var documentViewModel = frameworkElement.DataContext as ViewModel.DocumentViewModel;
            if( documentViewModel == null )
                return;
            ApplicationCommands.Find.Execute( null, documentViewModel.FileViewer );
        }

        /// <summary>
        /// Handle a click inside the match list view.
        /// 
        /// Implements match navigation inside an opened document.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void MatchListView_SelectionChanged( object sender, SelectionChangedEventArgs e )
        {
            ListView matchListView = (ListView) sender;

            // Ensure that the selected match is visible.
            var selectedMatch = matchListView.SelectedItem as ViewModel.OccurrenceViewModel;
            if( selectedMatch != null )
                matchListView.ScrollIntoView( selectedMatch );
            
            if( selectedMatch == null )
                return;

            // Update selection inside the document viewer, such that it corresponds to the selected match.
            var templatedParent = matchListView.TemplatedParent as ContentPresenter;
            var fileViewer = templatedParent.ContentTemplate.FindName( "fdsvFileViewer", templatedParent ) as TextViewerControl;
            if( fileViewer != null )
            {
                fileViewer.Select( selectedMatch.Column - 1, selectedMatch.Line - 1, selectedMatch.Match.Length );
                fileViewer.BringIntoView( fileViewer.SelectionStart, fileViewer.SelectionStart + fileViewer.SelectionLength, true );
            }
        }
        
        /// <summary>
        /// Handles a click on the first match navigation button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnFirstMatch_Click( object sender, RoutedEventArgs e )
        {
            // Simply select the first item in the match list. 
            // Highlighting and navigation is performed by the selection change event handler.
            var templatedParent = ( sender as FrameworkElement ).TemplatedParent as ContentPresenter;
            var matchListView = templatedParent.ContentTemplate.FindName( "lvMatches", templatedParent ) as ListView;
            matchListView.SelectedIndex = 0;
        }

        /// <summary>
        /// Handles a click on the previous match navigation button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnPreviousMatch_Click( object sender, RoutedEventArgs e )
        {
            // Simply select the previous item in the match list. 
            // Highlighting and navigation is performed by the selection change event handler.
            var templatedParent = ( sender as FrameworkElement ).TemplatedParent as ContentPresenter;
            var matchListView = templatedParent.ContentTemplate.FindName( "lvMatches", templatedParent ) as ListView;
            if( matchListView.SelectedIndex > 0 )
                matchListView.SelectedIndex = matchListView.SelectedIndex - 1;
        }

        /// <summary>
        /// Handles a click on the next match navigation button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnNextMatch_Click( object sender, RoutedEventArgs e )
        {
            // Simply select the next item in the match list. 
            // Highlighting and navigation is performed by the selection change event handler.
            var templatedParent = ( sender as FrameworkElement ).TemplatedParent as ContentPresenter;
            var matchListView = templatedParent.ContentTemplate.FindName( "lvMatches", templatedParent ) as ListView;
            if( matchListView.SelectedIndex < matchListView.Items.Count - 1 )
                matchListView.SelectedIndex = matchListView.SelectedIndex + 1;
        }

        /// <summary>
        /// Handles a click on the last match navigation button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnLastMatch_Click( object sender, RoutedEventArgs e )
        {
            // Simply select the last item in the match list. 
            // Highlighting and navigation is performed by the selection change event handler.
            var templatedParent = ( sender as FrameworkElement ).TemplatedParent as ContentPresenter;
            var matchListView = templatedParent.ContentTemplate.FindName( "lvMatches", templatedParent ) as ListView;
            matchListView.SelectedIndex = matchListView.Items.Count - 1;
        }

        /// <summary>
        /// Handles a click on the remove all directories button in the header of the directory list box.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnRemoveAllDirectories_Click( object sender, RoutedEventArgs e )
        {
            // Remove all directories from the list.
            var searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            var currentSearchViewModel = searchControlViewModel.CurrentSearch;
            if( currentSearchViewModel == null )
                return;

            if( dtaDirectories.SelectedCells.Count > 0 )
            {
                var selectedFilterEntries = new HashSet<ViewModel.DirectoryFilterEntryViewModel>();
                foreach( var selectedCell in dtaDirectories.SelectedCells )
                {
                    var selectedFilterEntry = selectedCell.Item as ViewModel.DirectoryFilterEntryViewModel;
                    if( selectedFilterEntry != null )
                        selectedFilterEntries.Add( selectedFilterEntry );
                }
                // If only a single entry or none has been selected still clear all entries.
                if( selectedFilterEntries.Count <= 1 )
                {
                    currentSearchViewModel.Directories.Clear();
                }
                else
                // Multiple entries have been selected thus delete the selected ones.
                {
                    foreach( var selectedFilterEntry in selectedFilterEntries )
                        currentSearchViewModel.Directories.Remove( selectedFilterEntry );
                }
                return;
            }
            else
            {
                currentSearchViewModel.Directories.Clear();
            }
            // This ensures, that the dummy row for adding a new list item is always visible and cannot be deleted.
            dtaDirectories.CanUserAddRows = false;
            dtaDirectories.CanUserAddRows = true;
        }

        /// <summary>
        /// Handles a click on the remove directory button in the directory list box.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnRemoveDirectory_Click( object sender, RoutedEventArgs e )
        {
            var searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            var currentSearchViewModel = searchControlViewModel.CurrentSearch;
            if( currentSearchViewModel == null )
                return;

            // Get the item, which corresponds to the button.
            var item = ( (Button) sender ).Tag as ViewModel.DirectoryFilterEntryViewModel;
            if( item != null )
            {
                // Remove the directory from the list.
                currentSearchViewModel.Directories.Remove( item );

            }
            // This ensures, that the dummy row for adding a new list item is always visible and cannot be deleted.
            dtaDirectories.CanUserAddRows = false;
            dtaDirectories.CanUserAddRows = true;
        }

        /// <summary>
        /// Handles a click on the jump to file button either in the match result list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void btnJumpToFile_Click( object sender, RoutedEventArgs e )
        {
            var searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            var currentSearchViewModel = searchControlViewModel.CurrentSearch;
            if( currentSearchViewModel == null )
                return;

            // Determine the file, which belongs to the button.
            var fileItem = ( (Button) sender ).Tag as string;
            if( fileItem != null )
            {
                // Add directory to the list.
                currentSearchViewModel.SelectedFile = new ViewModel.FileViewModel( fileItem );
                lbFiles.Focus();
                lbFiles.ScrollIntoView( lbFiles.SelectedItem );
            }
        }

        /// <summary>
        /// Handles a click on the open file button in the file list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void btnOpenFileClick( object sender, RoutedEventArgs e )
        {
            // Determine the file, which belongs to the button.
            var fileItem = ( (Button) sender ).Tag as string;
            if( fileItem != null )
            {
                // Open the file, the user clicked on.
                ViewModel.SearchControlViewModel viewModel = (ViewModel.SearchControlViewModel) this.DataContext;
                viewModel.OpenDocument( fileItem );
            }
        }

        /// <summary>
        /// Handles a click on the open file button in the search hit list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void btnOpenFileWithSearchHitsClick( object sender, RoutedEventArgs e )
        {
            // Open the file, the user clicked on and pass over the match occurrences, such that 
            // they can be highlighted.
            var searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            var currentSearchViewModel = searchControlViewModel.CurrentSearch;
            if( currentSearchViewModel == null )
                return;

            // Determine the file, which belongs to the button.
            var searchHitItem = ( (Button) sender ).Tag as ViewModel.SearchHitViewModel;
            if( searchHitItem != null )
            {
                searchControlViewModel.OpenDocument( searchHitItem.FilePath, currentSearchViewModel.CurrentSearchProcessor.SearchResultsQuery, searchHitItem.SearchHit.Occurrences );
                return;
            }

            var occurrenceItem = ( (Button) sender ).Tag as ViewModel.OccurrenceViewModel;
            if( occurrenceItem != null )
            {
                // Open the file, the user clicked on and pass over the match occurrences, such that 
                // they can be highlighted.
                searchControlViewModel.OpenDocument( occurrenceItem.FilePath, currentSearchViewModel.CurrentSearchProcessor.SearchResultsQuery, occurrenceItem.SearchHit.Occurrences, occurrenceItem.Occurrence );
                return;
            }
        }

        /// <summary>
        /// Handles a click on the add directory button either in the index file list or in the match result list.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void btnAddDirectory_Click( object sender, RoutedEventArgs e )
        {
            var searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            var currentSearchViewModel = searchControlViewModel.CurrentSearch;
            if( currentSearchViewModel == null )
                return; 

            // Determine the file, which belongs to the button.
            var fileItem = ( (Button) sender ).Tag as string;
            if( fileItem != null )
            {
                // Add directory to the list.
                currentSearchViewModel.Directories.Add( new ViewModel.DirectoryFilterEntryViewModel { Pattern = System.IO.Path.GetDirectoryName( fileItem ).Replace( "#", "[#]" ) } );
            }
        }

        /// <summary>
        /// Handles a click on a tag navigate to url button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnNavigateToUrl_Click( object sender, RoutedEventArgs e )
        {
            var btnNavigateToUrl = sender as Button;
            btnNavigateToUrl.ContextMenu.DataContext = btnNavigateToUrl.DataContext;
            btnNavigateToUrl.ContextMenu.IsOpen = true;
        }

        /// <summary>
        /// Handles a click on a menu item of the tag url navigator context menu.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ctxTagUrlNavigatorItem_Click( object sender, RoutedEventArgs e )
        {
            var menuItem = FindAncestor<MenuItem>( e.OriginalSource as DependencyObject );
            if( menuItem != null )
            {
                var tagLinkInfo = menuItem.DataContext as LinkInfo;
                if( tagLinkInfo != null )
                {
                    string url = tagLinkInfo.Url;
                    if( IsCodeXCavatorSearchUrl(url) )
                    {
                        string searchQuery = url.Substring( url.IndexOf( "://" ) + 3 ).Trim();
                        SearchContent( searchQuery );
                    }
                    else
                    {
                        System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo { UseShellExecute = true, FileName = url } );
                    }
                }
            }
        }

        /// <summary>
        /// Handles a click on a menu item of the document context menu.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ctxDocumentItem_Click( object sender, RoutedEventArgs e )
        {
            ContextMenu contextMenu = sender as ContextMenu;
            if( contextMenu != null )
            {
                string filePath = contextMenu.Tag as string;
                if( !string.IsNullOrEmpty( filePath ) )
                {
                    var menuItem = FindAncestor<MenuItem>( e.OriginalSource as DependencyObject );
                    if( menuItem != null )
                    {
                        var fileAction = menuItem.DataContext as IFileAction;
                        try
                        {
                            if( fileAction != null )
                                fileAction.Execute( filePath );
                        }
                        catch( Exception ex )
                        {
                            MessageBox.Show( string.Format( "An exception occurred while executing file action \"{0}\"!\n{1}", fileAction.Caption, ex ), "Execute file action...", MessageBoxButton.OK, MessageBoxImage.Error );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Checks, whether the search url is a CodeXCavator search query url.
        /// </summary>
        /// <param name="url">Url, which should be tested.</param>
        /// <returns>True, if the url is a search url, false otherwise.</returns>
        private static bool IsCodeXCavatorSearchUrl( string url )
        {
            return url.StartsWith( "xcavate://" ) ||
                   url.StartsWith( "xcave://" ) ||
                   url.StartsWith( "xcav://" );
        }

        /// <summary>
        /// Handles a click on a search tag button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void btnSearchTag_Click( object sender, RoutedEventArgs e )
        {
            var btnSearchTag = sender as Button;
            var tagInfo = btnSearchTag.DataContext as TagInfo;
            SearchTag( tagInfo );
        }

        /// <summary>
        /// Searches the for specific content.
        /// </summary>
        /// <param name="content">Content, which should be searched.</param>
        private void SearchContent( string content )
        {
            var searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            searchControlViewModel.SearchContent( content );

        }

        /// <summary>
        /// Searches the for specific content in a new tab.
        /// </summary>
        /// <param name="content">Content, which should be searched.</param>
        private void SearchContentInNewTab( string content )
        {
            var searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            searchControlViewModel.SearchContentInNewTab( content );
        }

        /// <summary>
        /// Searches for a specific tag.
        /// </summary>
        /// <param name="tagInfo">Tag info, of tag, which should be searched</param>
        private void SearchTag( TagInfo tagInfo )
        {
            ViewModel.SearchControlViewModel searchControlViewModel = (ViewModel.SearchControlViewModel) this.DataContext;
            var currentSearchViewModel = searchControlViewModel.CurrentSearch;
            if( currentSearchViewModel == null )
                return;

            if( tagInfo != null )
            {
                currentSearchViewModel.TagSearchProcessor.Search( tagInfo.Tag );
                currentSearchViewModel.CurrentSearchProcessorIndex = 1;
            }
        }

        /// <summary>
        /// Returns the tag contained in the specified highlighter token.
        /// </summary>
        /// <param name="run">Highlighter token, which should be searched for a tag.</param>
        /// <returns></returns>
        private static string GetTagFromHighlighterTokenText( string highlighterTokenText )
        {
            return Engine.Tokenizers.TagTokenizer.GetTag( highlighterTokenText );
        }

        /// <summary>
        /// Handles context opening and filling of the context menu of the file viewer.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void TextViewerControl_ContextMenuOpening( object sender, ContextMenuEventArgs e )
        {
            var textViewerControl = sender as TextViewerControl;
            if( textViewerControl != null )
            {
                bool inSelection = textViewerControl.SelectionContains( e.CursorLeft, e.CursorTop );
                if( inSelection )
                {
                    textViewerControl.ContextMenu = new ContextMenu();
                    textViewerControl.ContextMenu.Items.Add( new MenuItem { Command = ApplicationCommands.Copy } );
                    textViewerControl.ContextMenu.Items.Add( new Separator() );
                    textViewerControl.ContextMenu.Items.Add( new MenuItem { Command = ApplicationCommands.Find, CommandParameter = textViewerControl.SelectedText } );
                    textViewerControl.ContextMenu.Items.Add( new MenuItem { Command = FindCommands.FindInNewTab, CommandParameter = textViewerControl.SelectedText } );
                    textViewerControl.ContextMenu.Items.Add( new MenuItem { Command = FindCommands.FindInFile, CommandParameter = textViewerControl.SelectedText } ); 
                    return;
                }

                var textHitResult = textViewerControl.TextHitTest( e.CursorLeft, e.CursorTop );

                if( textHitResult != null && textHitResult.HighlighterTokenText != null && textHitResult.HighlighterToken.Type.Equals( "TAG", StringComparison.OrdinalIgnoreCase ) )
                {
                    string tag = GetTagFromHighlighterTokenText( textHitResult.HighlighterTokenText );
                    if( !string.IsNullOrEmpty( tag ) )
                    {
                        var searchControlViewModel = DataContext as ViewModel.SearchControlViewModel;
                        if( searchControlViewModel != null )
                        {
                            var currentSearchViewModel = searchControlViewModel.CurrentSearch;
                            if( currentSearchViewModel != null )
                            {
                                var tagInfo = currentSearchViewModel.CurrentIndex.Index.GetTagInfo( tag );
                                if( tagInfo != null && tagInfo.Links.Any() )
                                {
                                    textViewerControl.ContextMenu = FindResource( "ctxTagUrlNavigator" ) as ContextMenu;
                                    textViewerControl.ContextMenu.DataContext = tagInfo;
                                    return;
                                }
                            }
                        }
                    }
                }
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the TextMouseDown event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void fdsvFileViewer_TextMouseDown( object sender, TextHitEventArgs e )
        {
            if( e.TextHitInfo.HighlighterToken != null && e.TextHitInfo.HighlighterToken.Type.Equals( "TAG", StringComparison.OrdinalIgnoreCase ) )
            {
                string tag = GetTagFromHighlighterTokenText( e.TextHitInfo.HighlighterTokenText );
                if( !string.IsNullOrEmpty( tag ) )
                {
                    var searchControlViewModel = DataContext as ViewModel.SearchControlViewModel;
                    if( searchControlViewModel  != null )
                    {
                        var currentSearchViewModel = searchControlViewModel.CurrentSearch;
                        if( currentSearchViewModel != null )
                        {
                            SearchTag( currentSearchViewModel.CurrentIndex.Index.GetTagInfo( tag ) );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles the QueryCursor event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void fdsvFileViewer_QueryCursor( object sender, QueryCursorEventArgs e )
        {
            TextViewerControl textViewer = sender as TextViewerControl;
            if( textViewer != null )
            {
                var textHitInfo = textViewer.TextHitTest( e.GetPosition( textViewer ) );
                if( textHitInfo != null )
                {
                    if( textHitInfo.HighlighterToken != null && textHitInfo.HighlighterToken.Type == "TAG" || !string.IsNullOrEmpty( GetTagFromHighlighterTokenText( textHitInfo.HighlighterTokenText ) ) )
                    {
                        e.Cursor = Cursors.Hand;
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Handles the PreviewKeyDown event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void fdsvFileViewer_PreviewKeyDown( object sender, KeyEventArgs e )
        {
            TextViewerControl textViewer = sender as TextViewerControl;
            if( textViewer != null )
            {
                var textViewerContext = textViewer.DataContext as ViewModel.DocumentViewModel;
                if( textViewerContext != null )
                {
                    if( e.KeyboardDevice.IsKeyDown( Key.LeftCtrl ) || e.KeyboardDevice.IsKeyDown( Key.RightCtrl ) )
                    {
                        switch( e.Key )
                        {
                            case Key.Up:
                                {
                                    textViewerContext.GotoPreviousOccurrence();
                                    e.Handled = true;
                                }
                                break;
                            case Key.Down:
                                {
                                    textViewerContext.GotoNextOccurrence();
                                    e.Handled = true;
                                }
                                break;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Handles a click on the info image.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void imgInfo_MouseLeftButtonDown( object sender, MouseButtonEventArgs e )
        {
            ToolTip toolTip = imgInfo.ToolTip as ToolTip;
            toolTip.IsOpen = !toolTip.IsOpen;
        }

        /// <summary>
        /// Handles leave event on the info image.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void imgInfo_MouseLeave( object sender, MouseEventArgs e )
        {
            ToolTip toolTip = imgInfo.ToolTip as ToolTip;
            toolTip.IsOpen = false;
        }

        /// <summary>
        /// Handles a data context change.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_DataContextChanged( object sender, DependencyPropertyChangedEventArgs e )
        {
            var viewModel = e.NewValue as ViewModel.SearchControlViewModel;
            if( viewModel != null )
            {
                // Set the file viewer template.
                viewModel.FileViewerTemplate = Resources[ "dtpFileBrowserTab" ] as DataTemplate;
            }
        }

        /// <summary>
        /// Checks, whether the Application.Find command can be executed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void FindCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            var text = e.Parameter as string;
            e.CanExecute = ( !string.IsNullOrEmpty( text ) && text.IndexOfAny( new char[] { '\n', '\r' } ) < 0 );
            e.Handled = true;
        }

        /// <summary>
        /// Handles execution of the Application.Find command.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void FindCommand_Executed( object sender, ExecutedRoutedEventArgs e )
        {
            SearchContent( "\"" + ( e.Parameter as string ).Replace( "\"", "\\\"" ).Replace( "\\", @"\" ) + "\"" );
        }

        /// <summary>
        /// Checks, whether the InFileFindCommand can be executed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void InFileFindCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            FrameworkElement senderAsFrameworkElement = sender as FrameworkElement;
            var inFileSearchControl = senderAsFrameworkElement.FindName( "ucInFileSearch" ) as InFileSearchControl;
            if( e.Parameter == null && inFileSearchControl != null )
            {
                e.CanExecute = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Executes the InFileFindCommand
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void InFileFindCommand_Executed( object sender, ExecutedRoutedEventArgs e )
        {
            FrameworkElement senderAsFrameworkElement = sender as FrameworkElement;
            var inFileSearchControl = senderAsFrameworkElement.FindName( "ucInFileSearch" ) as InFileSearchControl;
            if( inFileSearchControl != null )
            {
                inFileSearchControl.Activate();
            }
        }

        /// <summary>
        /// Checks, whether the FindCommands.FindInFile command can be executed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void FindCommands_FindInFile_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            FrameworkElement senderAsFrameworkElement = sender as FrameworkElement;
            var inFileSearchControl = senderAsFrameworkElement.FindName( "ucInFileSearch" ) as InFileSearchControl;
            if( inFileSearchControl != null )
            {
                var text = e.Parameter as string;
                e.CanExecute = ( !string.IsNullOrEmpty( text ) && text.IndexOfAny( new char[] { '\n', '\r' } ) < 0 );
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles execution of the FindCommands.FindInFile command.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void FindCommands_FindInFile_Executed( object sender, ExecutedRoutedEventArgs e )
        {            
            FrameworkElement senderAsFrameworkElement = sender as FrameworkElement;
            var inFileSearchControl = senderAsFrameworkElement.FindName( "ucInFileSearch" ) as InFileSearchControl;
            if( inFileSearchControl != null )
            {
                inFileSearchControl.Find( ( e.Parameter as string ) );
            }
        }

        /// <summary>
        /// Checks, whether the FindCommands.FindInNewTab command can be executed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void FindCommands_FindInNewTab_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            var text = e.Parameter as string;
            e.CanExecute = ( !string.IsNullOrEmpty( text ) && text.IndexOfAny( new char[] { '\n', '\r' } ) < 0 );
            e.Handled = true;
        }

        /// <summary>
        /// Handles execution of the FindCommands.FindInNewTab command.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void FindCommands_FindInNewTab_Executed( object sender, ExecutedRoutedEventArgs e )
        {
            SearchContentInNewTab( "\"" + ( e.Parameter as string ).Replace( "\"", "\\\"" ).Replace( "\\", @"\" ) + "\"" );
        }


        /// <summary>
        /// Handles a double click on an item in the file list view.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void lbFiles_MouseDoubleClick( object sender, MouseButtonEventArgs e )
        {
            var doubleClickedItem = FindAncestor<ListBoxItem>( e.OriginalSource as DependencyObject );
            if( doubleClickedItem != null )
            {
                var fileViewModel = doubleClickedItem.DataContext as ViewModel.FileViewModel;
                var searchViewModel = this.DataContext as ViewModel.SearchControlViewModel;
                if( fileViewModel != null && searchViewModel != null )
                    searchViewModel.OpenDocument( fileViewModel.FilePath );
            }
        }

        /// <summary>
        /// Handles the click on the file type checkbox. 
        /// Implements toggling multiple selected items.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void chkFileTypeEnabled_Click( object sender, RoutedEventArgs e )
        {
            CheckBox checkBox = sender as CheckBox;
            if( checkBox != null )
            {
                var listBoxItem = FindAncestor<ListBoxItem>( checkBox );
                if( listBoxItem != null )
                {
                    if( lbFileTypes.SelectedItems.Contains( listBoxItem.DataContext ) )
                    {
                        foreach( ViewModel.FileTypeFilterItemViewModel selectedItem in lbFileTypes.SelectedItems )
                        {
                            selectedItem.Enabled = (bool) checkBox.IsChecked;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles a key press in the file type list box. 
        /// Implements toggling item checkbox via space key.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void lbFileTypes_PreviewKeyDown( object sender, KeyEventArgs e )
        {
            if( e.Key == Key.Space )
            {
                e.Handled = true;
                var selectedListBoxItem = lbFileTypes.SelectedItem as ViewModel.FileTypeFilterItemViewModel;
                selectedListBoxItem.Enabled = !selectedListBoxItem.Enabled;
                if( selectedListBoxItem != null )
                {
                    if( lbFileTypes.SelectedItems.Contains( selectedListBoxItem ) )
                    {
                        foreach( ViewModel.FileTypeFilterItemViewModel selectedItem in lbFileTypes.SelectedItems )
                        {
                            selectedItem.Enabled = selectedListBoxItem.Enabled;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Handles checkbox to automatically reopen this index on startup if no command line file is specified.
        /// </summary>
        public void SetReOpenNextTimeCheckBox()
        {
            if (mUserSettings.ReOpenLastFile == "true")
            {
                ReOpenNextTimeCheckBox.IsChecked = true;
            }
        }

        private void ReOpenNextTimeCheckBox_HandleCheck(object sender, RoutedEventArgs e)
        {
            mUserSettings.ReOpenLastFile = "true";
        }

        private void ReOpenNextTimeCheckBox_HandleUnchecked(object sender, RoutedEventArgs e)
        {
            mUserSettings.ReOpenLastFile = "false";
        }


        /// <summary>
        /// Handles key presses in search text boxes.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void searchTextBox_PreviewKeyDown( object sender, KeyEventArgs e )
        {
            switch( e.Key )
            {
                case Key.Enter:
                    {
                        TextBox textBox = sender as TextBox;
                        if( textBox != null )
                        {
                            ViewModel.SearchProcessor searchProcessor = textBox.Tag as ViewModel.SearchProcessor;
                            if( searchProcessor != null && !searchProcessor.ImmediateSearch )
                                searchProcessor.Search( textBox.Text );
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Handles double click on search results item.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void vwSearchResults_MouseDoubleClick( object sender, MouseButtonEventArgs e )
        {
            var searchControlViewModel = DataContext as ViewModel.SearchControlViewModel;
            if( searchControlViewModel != null )
            {
                var currentSearchViewModel = searchControlViewModel.CurrentSearch;
                if( currentSearchViewModel != null )
                {
                    var button = FindAncestor<ButtonBase>( e.OriginalSource as DependencyObject );
                    if( button != null )
                        return;
                    var item = FindAncestor<ListBoxItem>( e.OriginalSource as DependencyObject );
                    if( item != null )
                    {
                        var searchHit = item.DataContext as ViewModel.SearchHitViewModel;
                        if( searchHit != null )
                            searchControlViewModel.OpenDocument( searchHit.FilePath, currentSearchViewModel.CurrentSearchProcessor.SearchResultsQuery, searchHit.SearchHit.Occurrences );
                        var occurrence = item.DataContext as ViewModel.OccurrenceViewModel;
                        if( occurrence != null )
                            searchControlViewModel.OpenDocument( occurrence.SearchHit.FilePath, currentSearchViewModel.CurrentSearchProcessor.SearchResultsQuery, occurrence.SearchHit.Occurrences, occurrence.Occurrence );
                    }
                }
            }
        }

        private UserSettings mUserSettings;

        /// <summary>
        /// User settings
        /// </summary>
        public UserSettings UserSettings 
        {
            get { return mUserSettings; }
            set
            {
                if( mUserSettings != value )
                {
                    mUserSettings = value;
                    DataContext = new ViewModel.SearchControlViewModel( Indexes, UserSettings );
                }
            }
        }

        /// <summary>
        /// Handles loading of the file viewer control.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void fdsvFileViewer_Loaded( object sender, RoutedEventArgs e )
        {
            var textViewer = sender as TextViewerControl;
            if( textViewer != null )
                textViewer.Focus();
        }

        /// <summary>
        /// Handles loading of the in file search pane.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void ucInFileSearch_Loaded( object sender, RoutedEventArgs e )
        {
            var inFileSearch = (InFileSearchControl) sender;
            inFileSearch.TextSearcherTypes = Engine.TextSearchers.RegisteredTextSearcherTypes;
        }

        private void chkSortFilesAlphabetically_Checked(object sender, RoutedEventArgs e)
        {
            var fileListCollectionViewSource = FindResource("cvsFiles") as CollectionViewSource;
            if( fileListCollectionViewSource != null )
            {
                fileListCollectionViewSource.SortDescriptions.Add( new SortDescription(null, ListSortDirection.Ascending) );
            }
        }

        private void chkSortFilesAlphabetically_Unchecked(object sender, RoutedEventArgs e)
        {
            var fileListCollectionViewSource = FindResource("cvsFiles") as CollectionViewSource;
            if( fileListCollectionViewSource != null )
            {
                fileListCollectionViewSource.SortDescriptions.Clear();
            }
        }
    }
}
