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
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using System.ComponentModel;
using CodeXCavator.UI.ViewModel;

namespace CodeXCavator.UI
{
    /// <summary>
    /// In-file search control.
    /// 
    /// The in-file search control provides controls
    /// for searching within a single file.
    public partial class InFileSearchControl : UserControl, INotifyPropertyChanged
    {
        /// <summary>
        /// Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register( "Text", typeof( string ), typeof( InFileSearchControl ), new PropertyMetadata( null, OnTextPropertyChanged ) );

        /// <summary>
        /// Handles change of the Text dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnTextPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as InFileSearchControl;
            if( self != null )
                self.OnTextChanged( eventArgs.NewValue as string );
        }

        /// <summary>
        /// Handles change of the Text property.
        /// </summary>
        /// <param name="text">New text.</param>
        protected virtual void OnTextChanged( string text )
        {
            var contentAsFrameworkElement = Content as FrameworkElement;
            if( contentAsFrameworkElement != null )
            {
                var viewModel = contentAsFrameworkElement.DataContext as ViewModel.InFileSearchControlViewModel;
                if( viewModel == null )
                {
                    viewModel = new ViewModel.InFileSearchControlViewModel( text );
                    contentAsFrameworkElement.DataContext = viewModel;
                }
                else
                {
                    viewModel.Text = text;
                }
            }
        }

        /// <summary>
        /// C# Text dependency property wrapper.
        /// </summary>
        public string Text
        {
            get
            {
                return GetValue( TextProperty ) as string;
            }
            set
            {
                SetValue( TextProperty, value );
            }
        }

        /// <summary>
        /// TextSearcherTypes dependency property.
        /// </summary>
        public static readonly DependencyProperty TextSearcherTypesProperty = DependencyProperty.Register( "TextSearcherTypes", typeof( IEnumerable<Type> ), typeof( InFileSearchControl ), new PropertyMetadata( null, OnTextSearcherTypesPropertyChanged ) );

        /// <summary>
        /// Handles change of the TextSearcherTypes dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnTextSearcherTypesPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as InFileSearchControl;
            if( self != null )
                self.OnTextSearcherTypesChanged( eventArgs.NewValue as IEnumerable<Type> );
        }

        /// <summary>
        /// Handles change of the TextSearcherTypes property.
        /// </summary>
        /// <param name="iEnumerable"></param>
        protected virtual void OnTextSearcherTypesChanged( IEnumerable<Type> textSearchers )
        {
            foreach( var search in Searches )
            {
                search.TextSearchers = textSearchers.Select( textSearcherType => (ITextSearcher) Activator.CreateInstance( textSearcherType ) ).ToArray();
            }
        }

        /// <summary>
        /// C# TextSearcherTypes dependency property wrapper.
        /// </summary>
        public IEnumerable<Type> TextSearcherTypes
        {
            get
            {
                return GetValue( TextSearcherTypesProperty ) as IEnumerable<Type>;
            }
            set
            {
                SetValue( TextSearcherTypesProperty, value );
            }
        }

        /// <summary>
        /// Searches dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchesProperty = DependencyProperty.Register( "Searches", typeof( ObservableCollection<Search> ), typeof( InFileSearchControl ), new PropertyMetadata( null, OnSearchesPropertyChanged ) );

        /// <summary>
        /// Handles change of the Searches dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnSearchesPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as InFileSearchControl;
            if( self != null )
            {
                self.OnSearchesChanged( eventArgs.OldValue as ObservableCollection<Search>, eventArgs.NewValue as ObservableCollection<Search> );
            }
        }

        /// <summary>
        /// Handles change of the Searches property.
        /// </summary>
        /// <param name="iEnumerable"></param>
        protected virtual void OnSearchesChanged( ObservableCollection<Search> oldSearches, ObservableCollection<Search> newSearches )
        {
            if( oldSearches != null )
                oldSearches.CollectionChanged -= OnSearchesCollectionChanged;
            if( newSearches != null )
                newSearches.CollectionChanged += OnSearchesCollectionChanged;
        }

        private void OnSearchesCollectionChanged( object sender, NotifyCollectionChangedEventArgs e )
        {
            if( e.OldItems != null && e.OldItems.Contains( CurrentSearch ) )
                CurrentSearch = null;
            NotifyPropertyChanged( "CanAddSearch" );
        }

        /// <summary>
        /// C# Searches dependency property wrapper.
        /// </summary>
        public ObservableCollection<Search> Searches
        {
            get
            {
                return GetValue( SearchesProperty ) as ObservableCollection<Search>;
            }
            private set
            {
                SetValue( SearchesProperty, value );
            }
        }

        /// <summary>
        /// CurrentSearch dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentSearchProperty = DependencyProperty.Register( "CurrentSearch", typeof( Search ), typeof( InFileSearchControl ), new PropertyMetadata( null, OnCurrentSearchPropertyChanged ) );

        /// <summary>
        /// Handles change of the CurrentSearch dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnCurrentSearchPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as InFileSearchControl;
            if( self != null )
            {
                self.OnCurrentSearchChanged( eventArgs.OldValue as Search, eventArgs.NewValue as Search );
            }
        }

        /// <summary>
        /// Handles change of the CurrentSearch property.
        /// </summary>
        /// <param name="iEnumerable"></param>
        protected virtual void OnCurrentSearchChanged( Search oldCurrentSearch, Search newCurrentSearch )
        {
            if( newCurrentSearch != null )
            {
                foreach( var search in icSearches.Items )
                {
                    if( search == newCurrentSearch )
                    {
                        try
                        {
                            var searchContainer = icSearches.ItemContainerGenerator.ContainerFromItem( search ) as ContentPresenter;
                            var searchEdit = searchContainer.ContentTemplate.FindName( "tbSearchTerm", searchContainer ) as TextBox;
                            FocusManager.SetFocusedElement( this, searchEdit );
                        }
                        catch
                        {
                        }
                        break;
                    }
                }
                newCurrentSearch.CurrentOccurrence = newCurrentSearch.Occurrences != null ? newCurrentSearch.Occurrences.FirstOrDefault() : null;
            }
        }

        /// <summary>
        /// C# CurrentSearch dependency property wrapper.
        /// </summary>
        public Search CurrentSearch
        {
            get
            {
                return GetValue( CurrentSearchProperty ) as Search;
            }
            set
            {
                SetValue( CurrentSearchProperty, value );
            }
        }

        /// <summary>
        /// CurrentOccurrence dependency property
        /// </summary>
        public static readonly DependencyProperty CurrentOccurrenceProperty =
            DependencyProperty.Register( "CurrentOccurrence", typeof( IOccurrence ), typeof( InFileSearchControl ), new UIPropertyMetadata( null ) );

        /// <summary>
        /// C# ActiveOccurrence dependency property wrapper.
        /// </summary>
        public IOccurrence CurrentOccurrence
        {
            get { return (IOccurrence) GetValue( CurrentOccurrenceProperty ); }
            set { SetValue( CurrentOccurrenceProperty, value ); }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public InFileSearchControl()
        {
            Searches = new ObservableCollection<Search>();
            InitializeComponent();
        }

        /// <summary>
        /// Handles a click on the add search button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnAddSearchClicked( object sender, RoutedEventArgs e )
        {
            InsertEmptySearchAfter( ( (FrameworkElement) sender ).DataContext as Search );
        }

        /// <summary>
        /// Default search colors.
        /// These colors are used, when a new search is created to get different coloured searches.
        /// </summary>
        private static readonly Brush[] sDefaultSearchColors = { Brushes.Red, Brushes.Orange, Brushes.YellowGreen, Brushes.Green, Brushes.Lime, Brushes.Aqua, Brushes.Blue, Brushes.Violet };

        /// <summary>
        /// Index of the next default color to be used for a new search.
        /// </summary>
        private int mNextDefaultSearchColor;

        /// <summary>
        /// Inserts a new empty searcher after the specified search.
        /// </summary>
        /// <param name="search">Search after which a new search should be added.</param>
        private void InsertEmptySearchAfter( Search search )
        {
            var newSearch = CreateAndBindEmptySearch();
            int indexOfSearch = Searches.IndexOf( search ) + 1;
            Searches.Insert( indexOfSearch, newSearch );
        }

        /// <summary>
        /// Adds a new empty search to the end of the list.
        /// </summary>
        private void AppendEmptySearchToTheEnd()
        {
            var newSearch = CreateAndBindEmptySearch();
            Searches.Add( newSearch );
        }

        /// <summary>
        /// Creates a new empty search object and binds it to the text, which should be searched.
        /// </summary>
        /// <returns>The created search.</returns>
        private Search CreateAndBindEmptySearch()
        {
            var newSearch = new Search( TextSearcherTypes );

            newSearch.HighlightBrush = sDefaultSearchColors[mNextDefaultSearchColor];
            newSearch.SelectedTextSearcher = newSearch.TextSearchers.FirstOrDefault();

            var textBinding = new Binding( "Text" );
            textBinding.Source = this;
            BindingOperations.SetBinding( newSearch, Search.TextProperty, textBinding );

            var currentOccurrenceBinding = new Binding( "CurrentOccurrence" );
            currentOccurrenceBinding.Source = this;
            currentOccurrenceBinding.Mode = BindingMode.OneWayToSource;
            BindingOperations.SetBinding( newSearch, Search.CurrentOccurrenceProperty, currentOccurrenceBinding );

            mNextDefaultSearchColor = ( mNextDefaultSearchColor + 1 ) % sDefaultSearchColors.Length;
            
            return newSearch;
        }

        /// <summary>
        /// Handles the click on the delete search button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnDeleteSearchClicked( object sender, RoutedEventArgs e )
        {
            var senderAsFrameworkElement = sender as FrameworkElement;
            if( senderAsFrameworkElement != null )
            {
                var search = senderAsFrameworkElement.DataContext as Search;
                Searches.Remove( search );
            }
        }

        /// <summary>
        /// Handles a click on the color selector popup.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnColorSelectorClicked( object sender, MouseButtonEventArgs e )
        {
            Popup colorsPopup = ( (FrameworkElement) Content).Resources["popColors"] as Popup;
            if( colorsPopup != null )
            {
                colorsPopup.IsOpen = false;
                colorsPopup.PlacementTarget = sender as UIElement;
                colorsPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// Handles the mouse leaving the color selector popup.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnColorSelectorMouseLeave( object sender, MouseEventArgs e )
        {
            Popup colorsPopup = sender as Popup;
            if( colorsPopup != null )
            {
                colorsPopup.IsOpen = false;
            }
        }

        /// <summary>
        /// Handles a click on a color in the color selector.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event </param>
        private void OnColorSelectorColorClicked( object sender, MouseButtonEventArgs e )
        {
            Popup colorsPopup = ( (FrameworkElement) Content ).Resources["popColors"] as Popup;
            if( colorsPopup != null )
            {
                colorsPopup.IsOpen = false;
                var colorSelectorButton = (Rectangle) colorsPopup.PlacementTarget;
                colorSelectorButton.Fill = ( (Rectangle) sender ).Fill;
            }            
        }

        /// <summary>
        /// Search, which was focused the last time.
        /// </summary>
        private UIElement mLastFocusedSearch;

        /// <summary>
        /// Activates the in file search control.
        /// </summary>
        internal void Activate()
        {
            if( Searches.Count == 0 )
                AppendEmptySearchToTheEnd();
            FocusLastSearch();
        }

        /// <summary>
        /// Focuses the search, which was previously focused.
        /// </summary>
        private void FocusLastSearch()
        {
            this.Focus();

            if( mLastFocusedSearch != null && mLastFocusedSearch.IsVisible )
            {
                FocusManager.SetFocusedElement( this, mLastFocusedSearch );
                ( (TextBox) mLastFocusedSearch ).SelectAll();
                return;
            }
         
            try
            {
                mLastFocusedSearch = null;
                var searchContainer = icSearches.ItemContainerGenerator.ContainerFromIndex( Searches.Count - 1 ) as ContentPresenter;
                var count = VisualTreeHelper.GetChildrenCount( searchContainer );
                var searchEdit = searchContainer.ContentTemplate.FindName( "tbSearchTerm", searchContainer ) as TextBox;
                FocusManager.SetFocusedElement( this, searchEdit );
                searchEdit.SelectAll();
            }
            catch
            {
                // It might be that the content presenter has not created the visual tree yet...
            }
        }

        /// <summary>
        /// Handles the search term edito box getting the focus.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSearchTermGotFocus( object sender, RoutedEventArgs e )
        {
            mLastFocusedSearch = sender as UIElement;
            CurrentSearch = ( sender as FrameworkElement ).DataContext as Search;
        }

        /// <summary>
        /// Returns, whether a new search can be added.
        /// </summary>
        public bool CanAddSearch
        {
            get
            {
                return Searches.Count < 8;
            }
        }

        /// <summary>
        /// Notifies of property changes.
        /// </summary>
        /// <param name="property">Property, which changed.</param>
        protected virtual void NotifyPropertyChanged( string property )
        {
            if( PropertyChanged != null )
                PropertyChanged( this, new PropertyChangedEventArgs( property ) );
        }

        /// <summary>
        /// Property changed event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Handles a click on the search button.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSearchButtonClicked( object sender, RoutedEventArgs e )
        {
            DoSearch( sender );
        }

        /// <summary>
        /// Handles key events on the search term edit box.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSearchTermPreviewKeyDown( object sender, KeyEventArgs e )
        {
            if( e.Key == Key.Return || e.Key == Key.Enter )
            {
                DoSearch( sender );
                e.Handled = true;
            }

            if( e.KeyboardDevice.IsKeyDown( Key.LeftCtrl ) || e.KeyboardDevice.IsKeyDown( Key.RightCtrl ) )
            {
                switch( e.Key )
                {
                    case Key.Up: GotoPreviousOccurrence( sender ); e.Handled = true; break;
                    case Key.Down: GotoNextOccurrence( sender ); e.Handled = true; break;
                }
            }
        }

        /// <summary>
        /// Performs a search.
        /// </summary>
        /// <param name="searchTrigger">Search trigger.</param>
        private static void DoSearch( object searchTrigger )
        {
            var frameworkElement = searchTrigger as FrameworkElement;
            if( frameworkElement == null )
                return;
            var search = frameworkElement.DataContext as Search;
            if( search == null )
                return;
            if( !search.ImmediateSearch )
            {
                search.Find( search.SearchTerm, true );
            }
        }

        private static void GotoFirstOccurrence( object searchTrigger )
        {
            var frameworkElement = searchTrigger as FrameworkElement;
            if( frameworkElement == null )
                return;
            var search = frameworkElement.DataContext as Search;
            if( search == null )
                return;
            search.GotoFirstOccurrence();
        }

        private static void GotoPreviousOccurrence( object searchTrigger )
        {
            var frameworkElement = searchTrigger as FrameworkElement;
            if( frameworkElement == null )
                return;
            var search = frameworkElement.DataContext as Search;
            if( search == null )
                return;
            search.GotoPreviousOccurrence();
        }

        private static void GotoNextOccurrence( object searchTrigger )
        {
            var frameworkElement = searchTrigger as FrameworkElement;
            if( frameworkElement == null )
                return;
            var search = frameworkElement.DataContext as Search;
            if( search == null )
                return;
            search.GotoNextOccurrence();
        }

        private static void GotoLastOccurrence( object searchTrigger )
        {
            var frameworkElement = searchTrigger as FrameworkElement;
            if( frameworkElement == null )
                return;
            var search = frameworkElement.DataContext as Search;
            if( search == null )
                return;
            search.GotoLastOccurrence();
        }

        private void OnGotoFirstMatchClicked( object sender, RoutedEventArgs e )
        {
            GotoFirstOccurrence( sender );
        }

        private void OnGotoPreviousMatchClicked( object sender, RoutedEventArgs e )
        {
            GotoPreviousOccurrence( sender );
        }

        private void OnGotoNextMatchClicked( object sender, RoutedEventArgs e )
        {
            GotoNextOccurrence( sender );
        }

        private void OnGotoLastMatchClicked( object sender, RoutedEventArgs e )
        {
            GotoLastOccurrence( sender );
        }

        internal void Find( string content )
        {
            if( string.IsNullOrEmpty( content ) )
                return;

            if( Searches.Count == 0 )
                Activate();
            else
            if( CanAddSearch )
                AppendEmptySearchToTheEnd();

            FocusLastSearch();

            var lastSearch = Searches.LastOrDefault();
            if( lastSearch != null )
            {
                lastSearch.SearchTerm = content;
            }
        }
    }

    /// <summary>
    /// Single in file search.
    /// 
    /// The Search class represents a single in-file search. 
    /// </summary>
    public class Search : DependencyObject
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Search( IEnumerable<Type> textSearcherTypes )
        {
            TextSearchers = textSearcherTypes != null ? textSearcherTypes.Select( textSearcherType => (ITextSearcher) Activator.CreateInstance( textSearcherType ) ).ToArray() : null;
            HighlightBrush = Brushes.Red;
        }

        // Using a DependencyProperty as the backing store for ImmediateSearch.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ImmediateSearchProperty = DependencyProperty.Register( "ImmediateSearch", typeof( bool ), typeof( Search ), new UIPropertyMetadata( true, OnImmediateSearchPropertyChanged ) );

        /// <summary>
        /// Handles change of the SelectedTextSearcher dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnImmediateSearchPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as Search;
            if( self != null )
                self.OnImmediateSearchChanged( (bool) eventArgs.OldValue, (bool) eventArgs.NewValue );
        }

        /// <summary>
        /// Handles change of the SelectedTextSearcher property.
        /// </summary>
        /// <param name="SelectedTextSearcher">New SelectedTextSearcher.</param>
        protected virtual void OnImmediateSearchChanged( bool previousImmediateSearch, bool currentImmediateSearch )
        {
            Find( SearchTerm, currentImmediateSearch );
        }

        /// <summary>
        /// Indicates, whether search should be immediate
        /// </summary>
        public bool ImmediateSearch
        {
            get { return (bool) GetValue( ImmediateSearchProperty ); }
            set { SetValue( ImmediateSearchProperty, value ); }
        }

        /// <summary>
        /// TextSearchers dependency property.
        /// </summary>
        public static readonly DependencyProperty TextSearchersProperty = DependencyProperty.Register( "TextSearcherTypes", typeof( IEnumerable<ITextSearcher> ), typeof( Search ), new PropertyMetadata( null, OnTextSearchersPropertyChanged ) );

        /// <summary>
        /// Handles change of the TextSearchers dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnTextSearchersPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as Search;
            if( self != null )
                self.OnTextSearchersChanged( eventArgs.NewValue as IEnumerable<ITextSearcher> );
        }

        /// <summary>
        /// Handles change of the TextSearchers property.
        /// </summary>
        /// <param name="iEnumerable"></param>
        protected virtual void OnTextSearchersChanged( IEnumerable<ITextSearcher> textSearchers )
        {
            SelectedTextSearcher = TextSearchers.FirstOrDefault();
        }

        /// <summary>
        /// C# TextSearchers dependency property wrapper.
        /// </summary>
        public IEnumerable<ITextSearcher> TextSearchers
        {
            get
            {
                return GetValue( TextSearchersProperty ) as IEnumerable<ITextSearcher>;
            }
            set
            {
                SetValue( TextSearchersProperty, value );
            }
        }

        /// <summary>
        /// Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register( "Text", typeof( string ), typeof( Search ), new PropertyMetadata( null, OnTextPropertyChanged ) );

        /// <summary>
        /// Handles change of the Text dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnTextPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as Search;
            if( self != null )
                self.OnTextChanged( eventArgs.NewValue as string );
        }

        /// <summary>
        /// Handles change of the Text property.
        /// </summary>
        /// <param name="text">New text.</param>
        protected virtual void OnTextChanged( string text )
        {
        }

        /// <summary>
        /// C# Text dependency property wrapper.
        /// </summary>
        public string Text
        {
            get
            {
                return GetValue( TextProperty ) as string;
            }
            set
            {
                SetValue( TextProperty, value );
            }
        }

        /// <summary>
        /// SelectedTextSearcher dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedTextSearcherProperty = DependencyProperty.Register( "SelectedTextSearcher", typeof( ITextSearcher ), typeof( Search ), new PropertyMetadata( null, OnSelectedTextSearcherPropertyChanged ) );

        /// <summary>
        /// Handles change of the SelectedTextSearcher dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnSelectedTextSearcherPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as Search;
            if( self != null )
                self.OnSelectedTextSearcherChanged(  eventArgs.OldValue as ITextSearcher, eventArgs.NewValue as ITextSearcher );
        }

        /// <summary>
        /// Handles change of the SelectedTextSearcher property.
        /// </summary>
        /// <param name="SelectedTextSearcher">New SelectedTextSearcher.</param>
        protected virtual void OnSelectedTextSearcherChanged( ITextSearcher previousTextSearcher, ITextSearcher currentTextSearcher )
        {
            IOptionsProvider optionsProvider = previousTextSearcher as IOptionsProvider;
            if( optionsProvider != null )
            {
                optionsProvider.OptionChanged -= OnTextSearcherOptionChanged;
            }

            optionsProvider = currentTextSearcher as IOptionsProvider;
            if( optionsProvider != null )
            {
                optionsProvider.OptionChanged += OnTextSearcherOptionChanged;
            }

            Find( SearchTerm, ImmediateSearch );
        }

        /// <summary>
        /// C# SelectedTextSearcher dependency property wrapper.
        /// </summary>
        public ITextSearcher SelectedTextSearcher
        {
            get
            {
                return GetValue( SelectedTextSearcherProperty ) as ITextSearcher;
            }
            set
            {
                SetValue( SelectedTextSearcherProperty, value );
            }
        }

        /// <summary>
        /// Handles changes in the configuration of the current text searcher.
        /// </summary>
        /// <param name="optionsProvider">Text searcher as options provider.</param>
        /// <param name="option">Option, which changed.</param>
        private void OnTextSearcherOptionChanged( IOptionsProvider optionsProvider, IOption option )
        {
            Find( SearchTerm, ImmediateSearch );
        }

        /// <summary>
        /// Id of the search object.
        /// </summary>
        public string Id
        {
            get { return this.GetHashCode().ToString(); }
        }

        /// <summary>
        /// Search term dependency property.
        /// </summary>
        public static readonly DependencyProperty SearchTermProperty = DependencyProperty.Register( "SearchTerm", typeof( string ), typeof( Search ), new PropertyMetadata( null, OnSearchTermPropertyChanged ) );

        /// <summary>
        /// Handles change of the SearchTerm dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnSearchTermPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as Search;
            if( self != null )
                self.OnSearchTermChanged( eventArgs.NewValue as string );
        }

        /// <summary>
        /// Handles change of the Search property.
        /// </summary>
        /// <param name="iEnumerable"></param>
        protected virtual void OnSearchTermChanged( string search )
        {
            Find( search, ImmediateSearch );
        }

        /// <summary>
        /// C# SearchTerm dependency property wrapper.
        /// </summary>
        public string SearchTerm
        {
            get
            {
                return GetValue( SearchTermProperty ) as string;
            }
            set
            {
                SetValue( SearchTermProperty, value );
            }
        }

        /// <summary>
        /// Occurrences dependency property.
        /// </summary>
        public static readonly DependencyProperty OccurrencesProperty = DependencyProperty.Register( "Occurrences", typeof( IEnumerable<IOccurrence> ), typeof( Search ), new PropertyMetadata( null, OnOccurrencesPropertyChanged ) );

        /// <summary>
        /// Handles change of the Occurrences dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnOccurrencesPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as Search;
            if( self != null )
                self.OnOccurrencesChanged( eventArgs.NewValue as IEnumerable<IOccurrence> );
        }

        /// <summary>
        /// Handles change of the Occurrences property.
        /// </summary>
        /// <param name="iEnumerable"></param>
        protected virtual void OnOccurrencesChanged( IEnumerable<IOccurrence> occurrences )
        {
        }

        /// <summary>
        /// C# Occurrences dependency property wrapper.
        /// </summary>
        public IEnumerable<IOccurrence> Occurrences
        {
            get
            {
                return GetValue( OccurrencesProperty ) as IEnumerable<IOccurrence>;
            }
            set
            {
                SetValue( OccurrencesProperty, value as IEnumerable<IOccurrence> );
            }
        }

        /// <summary>
        /// CurrentOccurrence dependency property
        /// </summary>
        public static readonly DependencyProperty CurrentOccurrenceProperty =
            DependencyProperty.Register( "CurrentOccurrence", typeof( IOccurrence ), typeof( Search ), new UIPropertyMetadata( null ) );

        /// <summary>
        /// C# ActiveOccurrence dependency property wrapper.
        /// </summary>
        public IOccurrence CurrentOccurrence
        {
            get { return (IOccurrence) GetValue( CurrentOccurrenceProperty ); }
            set { SetValue( CurrentOccurrenceProperty, value ); }
        }

        /// <summary>
        /// HighlightBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register( "HighlightBrush", typeof( Brush ), typeof( Search ), new PropertyMetadata( null, OnHighlightBrushPropertyChanged ) );

        /// <summary>
        /// Handles change of the HighlightBrush dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnHighlightBrushPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            var self = sender as Search;
            if( self != null )
                self.OnHighlightBrushChanged( eventArgs.NewValue as Brush );
        }

        /// <summary>
        /// Handles change of the HighlightBrush property.
        /// </summary>
        /// <param name="HighlightBrush">New HighlightBrush.</param>
        protected virtual void OnHighlightBrushChanged( Brush HighlightBrush )
        {
        }

        /// <summary>
        /// C# HighlightBrush dependency property wrapper.
        /// </summary>
        public Brush HighlightBrush
        {
            get
            {
                return GetValue( HighlightBrushProperty ) as Brush;
            }
            set
            {
                SetValue( HighlightBrushProperty, value );
            }
        }

        /// <summary>
        /// Updates the current search results.
        /// </summary>
        public void Find( string searchTerm, bool immediateSearch )
        {
            if( SelectedTextSearcher == null || string.IsNullOrEmpty( searchTerm ) && immediateSearch )
            {
                Occurrences = null;
                return;
            }

            if( !SelectedTextSearcher.IsValidSearchQuery( searchTerm ) )
                throw new ArgumentException( string.Format( "Invalid search query: {0}", searchTerm ) );

            if( immediateSearch )
            {
                Occurrences = SelectedTextSearcher.Search( Text, searchTerm ).ToList();
            }
        }

        private int mCurrentOccurrenceIndex = -1;

        /// <summary>
        /// Jumps to first occurrence.
        /// </summary>
        internal void GotoFirstOccurrence()
        {
            if( Occurrences == null )
                return;
            var count = Occurrences.Count();
            if( count == 0 )
                return;

            mCurrentOccurrenceIndex = 0;

            CurrentOccurrence = Occurrences.ElementAt( mCurrentOccurrenceIndex );
        }

        /// <summary>
        /// Jumps to previous occurrence.
        /// </summary>
        internal void GotoPreviousOccurrence()
        {
            if( Occurrences == null )
                return;
            var count = Occurrences.Count();
            if( count == 0 )
                return;

            if( mCurrentOccurrenceIndex >= 0 )
            {
                --mCurrentOccurrenceIndex;
                if( mCurrentOccurrenceIndex < 0 )
                    mCurrentOccurrenceIndex = count - 1;
            }
            else
            {
                mCurrentOccurrenceIndex = count - 1;
            }
            CurrentOccurrence = Occurrences.ElementAt(mCurrentOccurrenceIndex);
        }

        /// <summary>
        /// Jumps to next occurrence.
        /// </summary>
        internal void GotoNextOccurrence()
        {
            if( Occurrences == null )
                return;
            var count = Occurrences.Count();
            if( count == 0 )
                return;
            if( mCurrentOccurrenceIndex >= 0 )
            {
                ++mCurrentOccurrenceIndex;
                if( mCurrentOccurrenceIndex >= count )
                    mCurrentOccurrenceIndex = 0;
            }
            else
            {
                mCurrentOccurrenceIndex = 0;
            }
            CurrentOccurrence = Occurrences.ElementAt( mCurrentOccurrenceIndex );
        }

        /// <summary>
        /// Jumps to last occurrence.
        /// </summary>
        internal void GotoLastOccurrence()
        {
            if( Occurrences == null )
                return;
            var count = Occurrences.Count();
            if( count == 0 )
                return;

            mCurrentOccurrenceIndex = count - 1;

            CurrentOccurrence = Occurrences.ElementAt( mCurrentOccurrenceIndex );
        }

    }

    /// <summary>
    /// Converts a text searcher or an enumeration of text searchers to a text searcher view model or an enumeration of text searcher view models.
    /// </summary>
    internal class TextSearcherToTextSearcherViewModelConverter : IValueConverter
    {

        public object Convert( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var textSearchers = value as IEnumerable<ITextSearcher>;
            if( textSearchers != null )
                return textSearchers.Select( textSearcher => new ViewModel.TextSearcherViewModel( textSearcher ) );

            return new ViewModel.TextSearcherViewModel( value as ITextSearcher );
        }

        public object ConvertBack( object value, Type targetType, object parameter, System.Globalization.CultureInfo culture )
        {
            var textSearcherViewModels = value as IEnumerable<ViewModel.TextSearcherViewModel>;
            if( textSearcherViewModels != null )
                return textSearcherViewModels.Select( viewModel => viewModel.TextSearcher );

            var textSearcherViewModel = value as ViewModel.TextSearcherViewModel;
            return textSearcherViewModel != null ? textSearcherViewModel.TextSearcher : null;
        }
    }

}
