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
using CodeXCavator.Engine;
using System.ComponentModel;
using CodeXCavator.Engine.Interfaces;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Media.TextFormatting;

namespace CodeXCavator.UI
{
    /// <summary>
    /// Text viewer control.
    /// 
    /// This control allows to view text. It's optimized for speed and for coping with
    /// large text files containing thousands of lines of code.
    /// It supports text coloring and text background highlighting. 
    /// Text can be selected via mouse and copied to the clipboard.
    /// It also displays line numbers column.
    /// </summary>
    public partial class TextViewerControl : UserControl, IDisposable
    {
        /// <summary>
        /// Selection changed routed event.
        /// </summary>
        public static RoutedEvent SelectionChangedEvent = EventManager.RegisterRoutedEvent( "SelectionChanged", RoutingStrategy.Bubble, typeof( RoutedEventHandler ), typeof( TextViewerControl ) );

        /// <summary>
        /// Selection changed event.
        /// 
        /// This event is raised every time the selection changes.
        /// </summary>
        public event RoutedEventHandler SelectionChanged
        {
            add { AddHandler( SelectionChangedEvent, value ); }
            remove { RemoveHandler( SelectionChangedEvent, value ); }
        }

        /// <summary>
        /// Static constructor.
        /// </summary>
        static TextViewerControl()        
        {
            // Register class event handlers for the text hit events.
            EventManager.RegisterClassHandler( typeof( TextViewerControl ), TextViewer.TextMouseUpEvent, new TextHitEventHandler( OnTextMouseUpEvent ) );
            EventManager.RegisterClassHandler( typeof( TextViewerControl ), TextViewer.TextMouseOverEvent, new TextHitEventHandler( OnTextMouseOverEvent ) );
            EventManager.RegisterClassHandler( typeof( TextViewerControl ), TextViewer.TextMouseDownEvent, new TextHitEventHandler( OnTextMouseDownEvent ) );
            // Register class event handlers for the line hit events.
            EventManager.RegisterClassHandler( typeof( TextViewerControl ), LineNumberViewer.LineMouseUpEvent, new LineHitEventHandler( OnLineMouseUpEvent ) );
            EventManager.RegisterClassHandler( typeof( TextViewerControl ), LineNumberViewer.LineMouseOverEvent, new LineHitEventHandler( OnLineMouseOverEvent ) );
            EventManager.RegisterClassHandler( typeof( TextViewerControl ), LineNumberViewer.LineMouseDownEvent, new LineHitEventHandler( OnLineMouseDownEvent ) );
        }

        /// <summary>
        /// Text viewer visual.
        /// </summary>
        private TextViewer mTextViewer;

        /// <summary>
        /// Mapping between highlight layers and color span layers.
        /// </summary>
        private Dictionary<HighlightLayer, ColorSpanLayer> mHighlightLayerColorSpanLayerMapping = new Dictionary<HighlightLayer, ColorSpanLayer>();

        /// <summary>
        /// Line number viewer visual.
        /// </summary>
        private LineNumberViewer mLineNumberViewer;

        /// <summary>
        /// Indicates, whether the user is currently selecting with the mouse.
        /// </summary>
        private bool mIsSelecting;
        /// <summary>
        /// Start position of selection, while selecting with the mouse.
        /// </summary>
        private int mMouseSelectionStart;
        /// <summary>
        /// Start line index, while selecting lines with the mouse.
        /// </summary>
        private int mMouseLineSelectionStart;
        /// <summary>
        /// End position of selection, while selecting with the mouse.
        /// </summary>
        private int mMouseSelectionEnd;
        /// <summary>
        /// End line index, while selecting lines with the mouse.
        /// </summary>
        private int mMouseLineSelectionEnd;

        /// <summary>
        /// TextMouseDown event.
        /// 
        /// The TextMouseDown event is raised every time a mouse button is pressed over a piece of text.
        /// </summary>
        public event TextHitEventHandler TextMouseDown
        {
            add { AddHandler( TextViewer.TextMouseDownEvent, value ); }
            remove { RemoveHandler( TextViewer.TextMouseDownEvent, value ); }
        }

        /// <summary>
        /// TextMouseOver event.
        /// 
        /// The TextMouseOver event is raised every time the mouse cursor hovers over a piece of text.
        /// </summary>
        public event TextHitEventHandler TextMouseOver
        {
            add { AddHandler( TextViewer.TextMouseOverEvent, value ); }
            remove { RemoveHandler( TextViewer.TextMouseOverEvent, value ); }
        }

        /// <summary>
        /// TextMouseUp event.
        /// 
        /// The TextMouseUp event is raised every time a mouse button is released over a piece of text.
        /// </summary>
        public event TextHitEventHandler TextMouseUp
        {
            add { AddHandler( TextViewer.TextMouseDownEvent, value ); }
            remove { RemoveHandler( TextViewer.TextMouseDownEvent, value ); }
        }

        /// <summary>
        /// LineMouseDown event.
        /// 
        /// The LineMouseDown event is raised every time a mouse button is pressed over a line.
        /// </summary>
        public event LineHitEventHandler LineMouseDown
        {
            add { AddHandler( LineNumberViewer.LineMouseDownEvent, value ); }
            remove { RemoveHandler( LineNumberViewer.LineMouseDownEvent, value ); }
        }

        /// <summary>
        /// LineMouseOver event.
        /// 
        /// The LineMouseOver event is raised every time the mouse cursor hovers over a line.
        /// </summary>
        public event LineHitEventHandler LineMouseOver
        {
            add { AddHandler( LineNumberViewer.LineMouseOverEvent, value ); }
            remove { RemoveHandler( LineNumberViewer.LineMouseOverEvent, value ); }
        }

        /// <summary>
        /// LineMouseUp event.
        /// 
        /// The LineMouseUp event is raised every time a mouse button is released over a line.
        /// </summary>
        public event LineHitEventHandler LineMouseUp
        {
            add { AddHandler( LineNumberViewer.LineMouseDownEvent, value ); }
            remove { RemoveHandler( LineNumberViewer.LineMouseDownEvent, value ); }
        }

        /// <summary>
        /// ShowLineNumbers dependency property.
        /// 
        /// This dependency property controls, whethe the line numbers should be shown, or not left to the text.
        /// </summary>
        public static readonly DependencyProperty ShowLineNumbersProperty = DependencyProperty.Register( "ShowLineNumbers", typeof( bool ), typeof( TextViewerControl ), new FrameworkPropertyMetadata( false, OnShowLineNumbersPropertyChanged ) );

        /// <summary>
        /// Handles change of the ShowLineNumbers dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnShowLineNumbersPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            TextViewerControl control = sender as TextViewerControl;
            if( control != null )
                control.OnShowLineNumbersChanged( (bool) eventArgs.NewValue );
        }

        /// <summary>
        /// Handles change of the ShowLineNumbers property.
        /// </summary>
        /// <param name="text">New text.</param>
        protected virtual void OnShowLineNumbersChanged( bool showLineNumbers )
        {
            mLineNumberViewer.Visibility = showLineNumbers ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// ShowLineNumbers property.
        /// 
        /// Defines, whether line numbes should be displayed left to the text.
        /// </summary>
        public bool ShowLineNumbers
        {
            get { return (bool) GetValue( ShowLineNumbersProperty ); }
            set { SetValue( ShowLineNumbersProperty, value ); }
        }

        /// <summary>
        /// Text dependency property.
        /// 
        /// This dependency property stores the text, which should be displayed.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register( "Text", typeof( string ), typeof( TextViewerControl ), new FrameworkPropertyMetadata( null, OnTextPropertyChanged ) );
        
        /// <summary>
        /// Handles change of the Text dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnTextPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            TextViewerControl control = sender as TextViewerControl;
            if( control != null )
                control.OnTextChanged( eventArgs.NewValue as string );
        }

        /// <summary>
        /// Handles change of the text property.
        /// </summary>
        /// <param name="text">New text.</param>
        protected virtual void OnTextChanged( string text )
        {
            mTextViewer.Text = text;
            mLineNumberViewer.LineCount = mTextViewer.LineCount;
            UpdateScrollbars();
        }

        /// <summary>
        /// Text property.
        /// 
        /// Text, which should be displayed by the control.
        /// </summary>
        public string Text
        {
            get { return GetValue( TextProperty ) as string; }
            set { SetValue( TextProperty, value ); }
        }

        /// <summary>
        /// Returns the number of lines in the current text.
        /// </summary>
        public int LineCount
        {
            get { return mTextViewer.LineCount; }
        }

        /// <summary>
        /// SelectionBrush dependency property.
        /// 
        /// This property changes the brush used for highlighting selected text.
        /// </summary>
        public static readonly DependencyProperty SelectionBrushProperty = DependencyProperty.Register( "SelectionBrush", typeof( Brush ), typeof( TextViewerControl ), new FrameworkPropertyMetadata( System.Windows.SystemColors.HighlightBrush, OnSelectionBrushPropertyChanged ) );

        /// <summary>
        /// Handles change of the SelectionBrush dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnSelectionBrushPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            TextViewerControl control = sender as TextViewerControl;
            if( control != null )
                control.OnSelectionBrushChanged( eventArgs.NewValue as Brush );
        }

        /// <summary>
        /// Handles change of the SelectionBrush property.
        /// </summary>
        /// <param name="selectionBrush ">New selection brush.</param>
        protected virtual void OnSelectionBrushChanged( Brush selectionBrush )
        {
            mTextViewer.SelectionBrush = selectionBrush;
        }

        /// <summary>
        /// SelectionBrush property.
        /// 
        /// Brush, which should be used for selection rendering.
        /// </summary>
        public Brush SelectionBrush
        {
            get { return GetValue( SelectionBrushProperty ) as Brush; }
            set { SetValue( SelectionBrushProperty, value ); }
        }

        /// <summary>
        /// SelectionBrush dependency property.
        /// 
        /// This property changes the brush used for highlighting selected text.
        /// </summary>
        public static readonly DependencyProperty SelectionOpacityProperty = DependencyProperty.Register( "SelectionOpacity", typeof( double ), typeof( TextViewerControl ), new FrameworkPropertyMetadata( 0.4, OnSelectionOpacityPropertyChanged ) );

        /// <summary>
        /// Handles change of the SelectionOpacity dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnSelectionOpacityPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            TextViewerControl control = sender as TextViewerControl;
            if( control != null )
                control.OnSelectionOpacityChanged( (double) eventArgs.NewValue );
        }

        /// <summary>
        /// Handles change of the SelectionOpacity property.
        /// </summary>
        /// <param name="selectionOpacity">New selection brush.</param>
        protected virtual void OnSelectionOpacityChanged( double selectionOpacity )
        {
            mTextViewer.SelectionOpacity = selectionOpacity;
        }

        /// <summary>
        /// SelectionBrush property.
        /// 
        /// Brush, which should be used for selection rendering.
        /// </summary>
        public double SelectionOpacity
        {
            get { return (double) GetValue( SelectionOpacityProperty ); }
            set { SetValue( SelectionOpacityProperty, value ); }
        }

        /// <summary>
        /// SelectionStart property.
        /// 
        /// Specifies or returns the start offset of the selection within the displayed text.
        /// </summary>
        public int SelectionStart
        {
            get { return mTextViewer.SelectionStart; }
            set
            {
                if( mTextViewer.SelectionStart != value )
                {
                    mTextViewer.SelectionStart = value;
                    RaiseEvent( new RoutedEventArgs( SelectionChangedEvent, this ) );
                }
            }
        }

        /// <summary>
        /// SelectionLength property.
        /// 
        /// Specifies or returns the length of the selection within the displayed text.
        /// </summary>
        public int SelectionLength
        {
            get { return mTextViewer.SelectionLength;  }
            set
            {
                if( mTextViewer.SelectionLength != value )
                {
                    mTextViewer.SelectionLength = value;
                    RaiseEvent( new RoutedEventArgs( SelectionChangedEvent, this ) );
                }
            }
        }

        /// <summary>
        /// SelectedText property.
        /// 
        /// Returns the currently selected text.
        /// </summary>
        public string SelectedText
        {
            get
            {
                return mTextViewer.SelectedText;
            }
        }

        /// <summary>
        /// Checks, whether the current selection contains the specified position.
        /// </summary>
        /// <param name="x">Horizontal position given in logical units relative to the origin of the text viewer control.</param>
        /// <param name="y">Vertical position given in logical units relative to the origin of the text viewer control.</param>
        /// <returns>True, if the selection contains the specified position, false otherwise.</returns>
        public bool SelectionContains( double x, double y )
        {
            return SelectionContains( new Point( x, y ) );
        }

        /// <summary>
        /// Checks, whether the current selection contains the specified position.
        /// </summary>
        /// <param name="position">Position given in logical units relative to the origin of the text viewer control.</param>
        /// <returns>True, if the selection contains the specified position, false otherwise.</returns>
        public bool SelectionContains( Point position )
        {
            var textViewerPosition = mTextViewer.TranslatePoint( position, this );
            var textOffset = mTextViewer.GetTextOffsetAtPosition( position );
            return ( textOffset >= mTextViewer.SelectionStart && textOffset < mTextViewer.SelectionStart + mTextViewer.SelectionLength );
        }

        /// <summary>
        /// Selects the specified text range.
        /// </summary>
        /// <param name="selectionStart">Start text offset of the selection range.</param>
        /// <param name="selectionLength">Length of the selection range.</param>
        public void Select( int selectionStart, int selectionLength )
        {
            if( selectionStart != mTextViewer.SelectionStart || selectionLength != mTextViewer.SelectionLength )
            {
                mTextViewer.Select( selectionStart, selectionLength );
                RaiseEvent( new RoutedEventArgs( SelectionChangedEvent, this ) );
            }
        }

        /// <summary>
        /// Selects the specified text range.
        /// </summary>
        /// <param name="selectionStartColumn">Column of the selection start.</param>
        /// <param name="selectionStartLine">Line of the selection start.</param>
        /// <param name="selectionEndColumn">Column of the selection end.</param>
        /// <param name="selectionEndLine">Line of the selection end.</param>
        public void Select( int selectionStartColumn, int selectionStartLine, int selectionEndColumn, int selectionEndLine )
        {
            var startOffset = mTextViewer.GetTextOffsetAtLineAndColumn( selectionStartColumn, selectionStartLine );
            var endOffset = mTextViewer.GetTextOffsetAtLineAndColumn( selectionEndColumn, selectionEndLine );
            if( startOffset != mTextViewer.SelectionStart || endOffset != mTextViewer.SelectionStart + mTextViewer.SelectionLength )
            {
                mTextViewer.Select( selectionStartColumn, selectionStartLine, selectionEndColumn, selectionEndLine );
                RaiseEvent( new RoutedEventArgs( SelectionChangedEvent, this ) );
            }
        }

        /// <summary>
        /// Selects the specified text range.
        /// </summary>
        /// <param name="selectionStartColumn">Column of the selection start.</param>
        /// <param name="selectionStartLine">Line of the selection start.</param>
        /// <param name="selectionLength">Length of the selection range.</param>
        public void Select( int selectionStartColumn, int selectionStartLine, int selectionLength )
        {
            var startOffset = mTextViewer.GetTextOffsetAtLineAndColumn( selectionStartColumn, selectionStartLine );
            var endOffset = startOffset + selectionLength;
            if( startOffset != mTextViewer.SelectionStart || endOffset != mTextViewer.SelectionStart + mTextViewer.SelectionLength )
            {
                mTextViewer.Select( selectionStartColumn, selectionStartLine, selectionLength );
                RaiseEvent( new RoutedEventArgs( SelectionChangedEvent, this ) );
            }
        }

        /// <summary>
        /// Selects all text.
        /// </summary>
        public void SelectAll()
        {
            if( Text != null )
                Select( 0, Text.Length );
            else
                Select( 0, 0 );
        }

        /// <summary>
        /// Deselects all text.
        /// </summary>
        public void DeselectAll()
        {
            Select( 0, 0 );
        }

        /// <summary>
        /// Copies the currently selected text to the clipboard.
        /// </summary>
        public void Copy()
        {
            string selectedText = SelectedText;
            if( !string.IsNullOrEmpty( selectedText ) )
            {
                Clipboard.SetText( selectedText );
            }
        }

        /// <summary>
        /// Handles the ApplicationCommand.Copy.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void CopyCommand_Executed( object sender, ExecutedRoutedEventArgs e )
        {
            Copy();
            e.Handled = true;
        }

        /// <summary>
        /// Determines, whether ApplicationCommand.Copy can be executed.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param
        private void CopyCommand_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = SelectionLength > 0;
            e.Handled = true;
        }
        
        /// <summary>
        /// Highlighter dependency property.
        /// 
        /// This dependency property stores the syntax highlighter, which should be used for highlighting the text.
        /// </summary>
        public static readonly DependencyProperty HighlighterProperty = DependencyProperty.Register( "Highlighter", typeof( IHighlighter ), typeof( TextViewerControl ), new FrameworkPropertyMetadata( null, OnHighlighterPropertyChanged ) );

        /// <summary>
        /// Handles change of the Highlighter dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnHighlighterPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            TextViewerControl control = sender as TextViewerControl;
            if( control != null )
                control.OnHighlighterChanged( eventArgs.NewValue as IHighlighter );
        }

        /// <summary>
        /// Handles change of the Highlighter property.
        /// </summary>
        /// <param name="highlighter">New highlighter.</param>
        protected virtual void OnHighlighterChanged( IHighlighter highlighter )
        {
            mTextViewer.Highlighter = highlighter;
        }

        /// <summary>
        /// Highlighter property.
        /// 
        /// Highlighter, which should be used for syntax highlighting by the text viewer control.
        /// </summary>
        public IHighlighter Highlighter
        {
            get { return GetValue( HighlighterProperty ) as IHighlighter; }
            set { SetValue( HighlighterProperty, value ); }
        }

        /// <summary>
        /// BackgroundHighlightLayers dependency property.
        /// 
        /// This dependency property stores a set of highlight layers, which should be used for highlighting the text.
        /// </summary>
        public static readonly DependencyProperty BackgroundHighlightLayersProperty = DependencyProperty.Register( "BackgroundHighlightLayers", typeof( FreezableCollection<HighlightLayer> ), typeof( TextViewerControl ), new FrameworkPropertyMetadata( null, OnBackgroundHighlightLayersPropertyChanged ) );


        /// <summary>
        /// Handles change of the BackgroundHighlightLayers dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnBackgroundHighlightLayersPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            TextViewerControl control = sender as TextViewerControl;
            if( control != null )
                control.OnBackgroundHighlightLayersChanged( eventArgs.OldValue as FreezableCollection<HighlightLayer>, eventArgs.NewValue as FreezableCollection<HighlightLayer> );
        }

        /// <summary>
        /// Handles change of the BackgroundHighlightLayers property.
        /// </summary>
        /// <param name="oldHighlightLayers">Old highlight layers</param>
        /// <param name="newHighlightLayers">New highlight layers</param>
        protected virtual void OnBackgroundHighlightLayersChanged( FreezableCollection<HighlightLayer> oldHighlightLayers, FreezableCollection<HighlightLayer> newHighlightLayers )
        {
            if( oldHighlightLayers != null )
            {                
                ( (INotifyCollectionChanged) newHighlightLayers ).CollectionChanged -= OnBackgroundHighlightLayerCollectionChanged;
            }
            if( newHighlightLayers != null )
            {
                ( (INotifyCollectionChanged) newHighlightLayers ).CollectionChanged += OnBackgroundHighlightLayerCollectionChanged;
            }
        }

        /// <summary>
        /// Handles the change of the background highlight layer collection.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnBackgroundHighlightLayerCollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
        {
            if( e.OldItems != null )
            {
                foreach( HighlightLayer oldHighlightLayer in e.OldItems )
                {
                    RemoveBackgroundHighlightLayerColorSpanMapping( oldHighlightLayer );
                    DependencyPropertyDescriptor.FromProperty( HighlightLayer.RangesProperty, typeof( HighlightLayer ) ).RemoveValueChanged( oldHighlightLayer, OnRangesOfBackgroundHighlightLayerChanged );
                    DependencyPropertyDescriptor.FromProperty( HighlightLayer.HighlightBrushProperty, typeof( HighlightLayer ) ).RemoveValueChanged( oldHighlightLayer, OnHighlightBrushOfHighlightLayerChanged );
                }
            }
            if( e.NewItems != null )
            {
                foreach( HighlightLayer newHighlightLayer in e.NewItems )
                {
                    AddBackgroundHighlightLayerColorSpanMapping( newHighlightLayer );
                    DependencyPropertyDescriptor.FromProperty( HighlightLayer.RangesProperty, typeof( HighlightLayer ) ).AddValueChanged( newHighlightLayer, OnRangesOfBackgroundHighlightLayerChanged );
                    DependencyPropertyDescriptor.FromProperty( HighlightLayer.HighlightBrushProperty, typeof( HighlightLayer ) ).AddValueChanged( newHighlightLayer, OnHighlightBrushOfHighlightLayerChanged );
                }
            }
        }

        /// <summary>
        /// Builds a color span layer from a highlight layer.
        /// </summary>
        /// <param name="newHighlightLayer">Highlight layer, from which a color span layer should be created.</param>
        /// <returns>ColorSpanLayer instance created from the highlight layer.</returns>
        private static ColorSpanLayer BuildColorSpanLayerFromHighlightLayer( HighlightLayer newHighlightLayer )
        {
            var colorSpanLayer = new ColorSpanLayer();
            colorSpanLayer.DefaultBrush = newHighlightLayer.HighlightBrush;
            if( newHighlightLayer.Ranges != null )
            {
                foreach( var range in newHighlightLayer.Ranges )
                    colorSpanLayer.Add( range.Item2, range.Item1, range.Item3 );
            }
            return colorSpanLayer;
        }

        /// <summary>
        /// Creates a color span layer from the highlight layer, and a mapping between the highlight layer and the color span layer.
        /// </summary>
        /// <param name="newHighlightLayer">Highlight layer, from which a color span layer should be createad and mapped to.</param>
        private void AddBackgroundHighlightLayerColorSpanMapping( HighlightLayer newHighlightLayer )
        {
            var colorSpanLayer = BuildColorSpanLayerFromHighlightLayer( newHighlightLayer );
            mHighlightLayerColorSpanLayerMapping[newHighlightLayer] = colorSpanLayer;
            mTextViewer.BackgroundHighlightColorSpanLayers.Add( colorSpanLayer );
        }

        /// <summary>
        /// Removes a color span layer belonging to the specified highlight layer.
        /// </summary>
        /// <param name="oldHighlightLayer">Highlight layer, whose color span layer should be removed.</param>
        private void RemoveBackgroundHighlightLayerColorSpanMapping( HighlightLayer oldHighlightLayer )
        {
            ColorSpanLayer colorSpanLayerOfBackgroundHighlightLayer = null;
            if( mHighlightLayerColorSpanLayerMapping.TryGetValue( oldHighlightLayer, out colorSpanLayerOfBackgroundHighlightLayer ) )
            {
                mHighlightLayerColorSpanLayerMapping.Remove( oldHighlightLayer );
                mTextViewer.BackgroundHighlightColorSpanLayers.Remove( colorSpanLayerOfBackgroundHighlightLayer );
            }
        }

        /// <summary>
        /// Handles change of the background highlight layer ranges.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event arguments.</param>
        private void OnRangesOfBackgroundHighlightLayerChanged( object sender, EventArgs args )
        {
            var highlightLayer = sender as HighlightLayer;
            RemoveBackgroundHighlightLayerColorSpanMapping( highlightLayer );
            AddBackgroundHighlightLayerColorSpanMapping( highlightLayer );
        }

        /// <summary>
        /// Handles change of brush of highlight layer.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="args">Event arguments.</param>
        private void OnHighlightBrushOfHighlightLayerChanged( object sender, EventArgs args )
        {
            var highlightLayer = sender as HighlightLayer;
            ColorSpanLayer colorSpanLayer = null;
            if( mHighlightLayerColorSpanLayerMapping.TryGetValue( highlightLayer, out colorSpanLayer ) )
            {
                colorSpanLayer.DefaultBrush = highlightLayer.HighlightBrush;
            }
        }

        /// <summary>
        /// BackgroundHighlightLayers property.
        /// 
        /// BackgroundHighlightLayers, which should be used for background highlighting.
        /// </summary>
        public FreezableCollection<HighlightLayer> BackgroundHighlightLayers
        {
            get { return GetValue( BackgroundHighlightLayersProperty ) as FreezableCollection<HighlightLayer>; }
            set { SetValue( BackgroundHighlightLayersProperty, value ); }
        }

        /// <summary>
        /// Collection of foreground highlight color span layers.
        /// </summary>
        [Browsable( false )]
        public ObservableCollection<ColorSpanLayer> ForegroundHighlightColorSpanLayers 
        {
            get { return mTextViewer.ForegroundHighlightColorSpanLayers; }
        }

        /// <summary>
        /// Collection of background highlight color span layers.
        /// </summary>
        [Browsable( false )]
        public ObservableCollection<ColorSpanLayer> BackgroundHighlightColorSpanLayers
        {
            get { return mTextViewer.BackgroundHighlightColorSpanLayers; }
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public TextViewerControl()
        {
            BackgroundHighlightLayers = new FreezableCollection<HighlightLayer>();

            InitializeComponent();
            InitializeTextViewer();
            InitializeLineNumberViewer();
            UpdateScrollbars();
        }

        /// <summary>
        /// Initializes the text viewer.
        /// </summary>
        private void InitializeTextViewer()
        {
            mTextViewer = new TextViewer( this );
            mTextViewer.SetValue( Grid.RowProperty, 0 );
            mTextViewer.SetValue( Grid.ColumnProperty, 1 );
            mTextViewer.SizeChanged += new SizeChangedEventHandler( OnTextViewerSizeChanged );
            grdViewer.Children.Add( mTextViewer );
        }

        /// <summary>
        /// Initializes the line number pane.
        /// </summary>
        private void InitializeLineNumberViewer()
        {
            mLineNumberViewer = new LineNumberViewer( this );
            mLineNumberViewer.Margin = new Thickness( 0, 0, 2, 0 );
            mLineNumberViewer.SetValue( Grid.RowProperty, 0 );
            mLineNumberViewer.SetValue( Grid.ColumnProperty, 0 );
            mLineNumberViewer.Visibility = ShowLineNumbers ? Visibility.Visible : Visibility.Collapsed;
            grdViewer.Children.Add( mLineNumberViewer );
        }

        /// <summary>
        /// Brings the specified position into the view of the text viewer control.
        /// </summary>
        /// <param name="lineIndexOrTextOffset">Position, which should be made visible. This might be a line or a text offset.</param>
        /// <param name="isTextOffset">If this parameter is set to true the position is a text offset, otherwise it is a line index.</param>
        public void BringIntoView( int lineIndexOrTextOffset, bool isTextOffset = false )
        {
            if( isTextOffset )
                BringTextRangeIntoView( lineIndexOrTextOffset, lineIndexOrTextOffset );
            else
                BringLineRangeIntoView( lineIndexOrTextOffset, lineIndexOrTextOffset );
        }

        /// <summary>
        /// Brings the specified range into the view of the text viewer control.
        /// </summary>
        /// <param name="startLineIndexOrTextOffset">Start position of the range, which should be made visible. This might be a line or a text offset.</param>
        /// <param name="endLineIndexOrTextOffset">End position of the range, which should be made visible. This might be a line or a text offset.</param>
        /// <param name="isTextOffset">If this parameter is set to true the positions are a text offset, otherwise they are a line index.</param>
        public void BringIntoView( int startLineIndexOrTextOffset, int endLineIndexOrTextOffset, bool isTextOffset = false )
        {
            if( isTextOffset )
                BringTextRangeIntoView( startLineIndexOrTextOffset, endLineIndexOrTextOffset );
            else
                BringLineRangeIntoView( startLineIndexOrTextOffset, endLineIndexOrTextOffset );
        }

        /// <summary>
        /// Brings a range of lines into the current view.
        /// </summary>
        /// <param name="startLineIndex">Start line index of the line range.</param>
        /// <param name="endLineIndex">End line index of the line range.</param>
        protected void BringLineRangeIntoView( int startLineIndex, int endLineIndex )
        {            
            int viewportStartLineIndex = mTextViewer.TopLine;
            int viewportEndLineIndex = mTextViewer.TopLine + (int) mTextViewer.VerticalViewportSize - 1;

            bool startIsInside = startLineIndex >= viewportStartLineIndex && startLineIndex < viewportEndLineIndex;
            bool endIsInside = endLineIndex >= viewportStartLineIndex && endLineIndex < viewportEndLineIndex;

            // Skip if range is visible
            if( startIsInside && endIsInside )
                return;

            int deltaStart = startLineIndex - viewportStartLineIndex;
            int deltaEnd = endLineIndex - viewportEndLineIndex;

            if( Math.Abs( deltaStart ) < Math.Abs( deltaEnd ) )
            {
                // Bring start of the range into view.
                sbVertical.Value = sbVertical.Value + deltaStart;
            }
            else
            {
                // Bring end of the range into view.
                sbVertical.Value = sbVertical.Value + deltaEnd;
            }
        }

        /// <summary>
        /// Brings a text range into the current view.
        /// </summary>
        /// <param name="startOffset">Start offset of the text range.</param>
        /// <param name="endOffset">End offset of the text range.</param>
        protected void BringTextRangeIntoView( int startOffset, int endOffset )
        {
            var startColumnAndLine = TextUtilities.OffsetToColumnAndLineIndex( startOffset, mTextViewer.LineOffsets );
            var endColumnAndLine = TextUtilities.OffsetToColumnAndLineIndex( endOffset, mTextViewer.LineOffsets );
            if( startColumnAndLine.Line >= 0 && endColumnAndLine.Line >= 0 )
            {
                // Scroll vertically
                BringLineRangeIntoView( startColumnAndLine.Line, endColumnAndLine.Line );
                // Scroll horizontally                
                var horizontalExtents = mTextViewer.GetHorizontalExtentsOfTextRange( startOffset, endOffset );

                double viewportLeft = mTextViewer.HorizontalScrollPosition;
                double viewportRight = viewportLeft + mTextViewer.HorizontalViewportSize;

                bool leftIsInside = horizontalExtents.Item1 >= viewportLeft && horizontalExtents.Item1 < viewportRight;
                bool rightIsInside = horizontalExtents.Item2 >= viewportLeft && horizontalExtents.Item2 < viewportRight;

                // Skip if range is visible
                if( leftIsInside && rightIsInside )
                    return;

                double deltaLeft = horizontalExtents.Item1 - viewportLeft;
                double deltaRight = horizontalExtents.Item2 - viewportRight;

                if( Math.Abs( deltaLeft ) < Math.Abs( deltaRight ) )
                {
                    // Bring start of the range into view.
                    sbHorizontal.Value = sbHorizontal.Value + deltaLeft;
                }
                else
                {
                    // Bring end of the range into view.
                    sbHorizontal.Value = sbHorizontal.Value + deltaRight;
                }
            }
        }

        /// <summary>
        /// Handles the size change of the text viewer visual.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnTextViewerSizeChanged( object sender, SizeChangedEventArgs e )
        {
            UpdateViewportSizeOfScrollbars();
        }

        private const double HORIZONTAL_LARGE_SCROLL_CHANGE_VIEWPORT_RATIO = 4.0;
        private const double VERTICAL_LARGE_SCROLL_CHANGE_VIEWPORT_RATIO = 2.0;

        /// <summary>
        /// Updates the scrollbar state according to the viewport size and number of lines in the text and the maximum text width.
        /// </summary>
        private void UpdateScrollbars()
        {
            sbHorizontal.Minimum = 0;
            sbHorizontal.Maximum = Math.Max( mTextViewer.HorizontalTextSize - mTextViewer.HorizontalViewportSize, 0.0 );
            sbHorizontal.SmallChange = mTextViewer.AverageCharWidth;
            sbVertical.Minimum = 0;
            sbVertical.Maximum = mTextViewer.LineCount;
            sbVertical.SmallChange = 1.0;

            UpdateViewportSizeOfScrollbars();
        }

        /// <summary>
        /// Updates the viewport size of the scrollbars according to the viewport of the text viewer.
        /// </summary>
        private void UpdateViewportSizeOfScrollbars()
        {
            sbHorizontal.Maximum = Math.Max( mTextViewer.HorizontalTextSize - mTextViewer.HorizontalViewportSize, 0.0 );
            sbHorizontal.ViewportSize = mTextViewer.HorizontalViewportSize;
            sbHorizontal.LargeChange = sbHorizontal.ViewportSize / HORIZONTAL_LARGE_SCROLL_CHANGE_VIEWPORT_RATIO;
            sbVertical.ViewportSize = mTextViewer.VerticalViewportSize;
            sbVertical.LargeChange = sbVertical.ViewportSize / VERTICAL_LARGE_SCROLL_CHANGE_VIEWPORT_RATIO;
        }

        private const double VERTICAL_MOUSEWHEEL_SCROLL_CHANGE = 3.0;

        /// <summary>
        /// Handles mouse wheel events.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void OnPreviewMouseWheel( object sender, MouseWheelEventArgs e )
        {
            if( e.Delta > 0 )
                sbVertical.Value -= VERTICAL_MOUSEWHEEL_SCROLL_CHANGE;
            if( e.Delta < 0 )
                sbVertical.Value += VERTICAL_MOUSEWHEEL_SCROLL_CHANGE;                
        }

        /// <summary>
        /// Handles vertical scroll events.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnVerticalScrollBarValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            mTextViewer.TopLine = (int) Math.Floor( e.NewValue );
            mLineNumberViewer.TopLine = mTextViewer.TopLine;
        }

        /// <summary>
        /// Handles horizontal scroll events.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments.</param>
        private void OnHorizontalScrollBarValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            mTextViewer.HorizontalScrollPosition = e.NewValue;
        }

        /// <summary>
        /// Handles the MouseDown event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected override void OnMouseDown( MouseButtonEventArgs e )
        {
            base.OnMouseDown( e );
            this.Focus();
        }

        /// <summary>
        /// Handles the KeyDown event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if( e.IsDown )
            {
                switch( e.Key )
                {
                    // Select all
                    case Key.A:
                        {
                            if( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                            {
                                SelectAll();
                                e.Handled = true;
                            }
                        }
                        break;
                    // Jump to beginning of the text.
                    case Key.Home:
                        {
                            if( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                            {
                                BringIntoView( 0 );
                                e.Handled = true;
                            }
                        }
                        break;
                    // Jump to the end of the text.
                    case Key.End:
                        {
                            if( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                            {
                                BringIntoView( LineCount - 1 );
                                e.Handled = true;
                            }
                        }
                        break;
                    // Scroll up - line wise
                    case Key.Up:
                        {
                            sbVertical.Value = sbVertical.Value - 1;
                            e.Handled = true;
                        }
                        break;
                    // Scroll down - line wise
                    case Key.Down:
                        {
                            sbVertical.Value = sbVertical.Value + 1;
                            e.Handled = true;
                        }
                        break;
                    // Scroll up - page wise
                    case Key.PageUp:
                        {
                            sbVertical.Value = sbVertical.Value - (int) Math.Floor( mTextViewer.VerticalViewportSize );
                            e.Handled = true;
                        }
                        break;
                    // Scroll down - page wise
                    case Key.PageDown:
                        {
                            sbVertical.Value = sbVertical.Value + (int) Math.Floor( mTextViewer.VerticalViewportSize );
                            e.Handled = true;                            
                        }
                        break;

                    case Key.Left:
                        {
                            if( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                                sbHorizontal.Value = sbHorizontal.Value - mTextViewer.HorizontalViewportSize;
                            else
                                sbHorizontal.Value = sbHorizontal.Value - sbHorizontal.SmallChange;
                            e.Handled = true;
                        }
                        break;

                    case Key.Right:
                        {
                            if( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                                sbHorizontal.Value = sbHorizontal.Value + mTextViewer.HorizontalViewportSize;
                            else
                                sbHorizontal.Value = sbHorizontal.Value + sbHorizontal.SmallChange;
                            e.Handled = true;
                        }
                        break;

                    // Copy to clipboard.
                    case Key.C:
                    case Key.Insert:
                        {
                            if( Keyboard.IsKeyDown( Key.LeftCtrl ) || Keyboard.IsKeyDown( Key.RightCtrl ) )
                            {
                                Copy();
                                e.Handled = true;
                            }
                        }
                        break;
                }
            }
            base.OnKeyUp( e );
        }

        /// <summary>
        /// Handles the TextMouseDown routed event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void OnTextMouseDownEvent( object sender, TextHitEventArgs e )
        {
            var control = sender as TextViewerControl;
            if( control != null )
            {
                control.OnTextMouseDown( e );
            }
        }

        /// <summary>
        /// Handles the TextMouseDown event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected virtual void OnTextMouseDown( TextHitEventArgs e )
        {
            if( e.TextHitInfo.TextOffset >= 0 )
            {
                if( e.MouseEventArgs.LeftButton == MouseButtonState.Pressed )
                {
                    mIsSelecting = true;
                    mMouseSelectionStart = e.TextHitInfo.TextOffset;
                    mMouseSelectionEnd = e.TextHitInfo.TextOffset;
                    Select( e.TextHitInfo.TextOffset, 0 );
                    // Capture mouse with text viewer.
                    e.MouseEventArgs.MouseDevice.Capture( mTextViewer );
                }
            }
        }

        /// <summary>
        /// Handles the TextMouseOver routed event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void OnTextMouseOverEvent( object sender, TextHitEventArgs e )
        {
            var control = sender as TextViewerControl;
            if( control != null )
                control.OnTextMouseOver( e );
        }

        /// <summary>
        /// Handles the TextMouseOver event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected virtual void OnTextMouseOver( TextHitEventArgs e )
        {
            if( e.TextHitInfo.TextOffset >= 0 )
            {
                if( e.MouseEventArgs.LeftButton == MouseButtonState.Pressed )
                {
                    mMouseSelectionEnd = e.TextHitInfo.TextOffset;

                    if( mMouseSelectionEnd >= mMouseSelectionStart )
                        Select( mMouseSelectionStart, mMouseSelectionEnd - mMouseSelectionStart );
                    else
                        Select( mMouseSelectionEnd, mMouseSelectionStart - mMouseSelectionEnd );
                }
            }
        }

        /// <summary>
        /// Handles the TextMouseUp routed event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void OnTextMouseUpEvent( object sender, TextHitEventArgs e )
        {
            var control = sender as TextViewerControl;
            if( control != null )
                control.OnTextMouseUp( e );
        }

        /// <summary>
        /// Handles the TextMouseUp event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected virtual void OnTextMouseUp( TextHitEventArgs e )
        {
        }

        /// <summary>
        /// Handles the LineMouseDown routed event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void OnLineMouseDownEvent( object sender, LineHitEventArgs e )
        {
            var control = sender as TextViewerControl;
            if( control != null )
            {
                control.OnLineMouseDown( e );
            }
        }

        /// <summary>
        /// Handles the LineMouseDown event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected virtual void OnLineMouseDown( LineHitEventArgs e )
        {
            if( e.LineIndex >= 0 )
            {
                if( e.MouseEventArgs.LeftButton == MouseButtonState.Pressed )
                {
                    mIsSelecting = true;
                    mMouseLineSelectionStart = e.LineIndex;
                    mMouseLineSelectionEnd = e.LineIndex;
                    Select( mTextViewer.LineOffsets[mMouseLineSelectionStart], mTextViewer.LineOffsets[mMouseLineSelectionEnd + 1] - mTextViewer.LineOffsets[mMouseLineSelectionStart] );
                    // Capture mouse with Line viewer.
                    e.MouseEventArgs.MouseDevice.Capture( mLineNumberViewer );
                }
            }
        }

        /// <summary>
        /// Handles the LineMouseOver routed event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void OnLineMouseOverEvent( object sender, LineHitEventArgs e )
        {
            var control = sender as TextViewerControl;
            if( control != null )
                control.OnLineMouseOver( e );
        }

        /// <summary>
        /// Handles the LineMouseOver event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected virtual void OnLineMouseOver( LineHitEventArgs e )
        {
            if( e.LineIndex >= 0 )
            {
                if( e.MouseEventArgs.LeftButton == MouseButtonState.Pressed )
                {
                    mMouseLineSelectionEnd = e.LineIndex;
                    if( mMouseLineSelectionEnd >= mMouseLineSelectionStart )
                        Select( mTextViewer.LineOffsets[mMouseLineSelectionStart], mTextViewer.LineOffsets[mMouseLineSelectionEnd + 1] - mTextViewer.LineOffsets[mMouseLineSelectionStart] );
                    else
                        Select( mTextViewer.LineOffsets[mMouseLineSelectionEnd], mTextViewer.LineOffsets[mMouseLineSelectionStart + 1] - mTextViewer.LineOffsets[mMouseLineSelectionEnd] );
                }
            }
        }

        /// <summary>
        /// Handles the LineMouseUp routed event.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private static void OnLineMouseUpEvent( object sender, LineHitEventArgs e )
        {
            var control = sender as TextViewerControl;
            if( control != null )
                control.OnLineMouseUp( e );
        }

        /// <summary>
        /// Handles the LineMouseUp event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected virtual void OnLineMouseUp( LineHitEventArgs e )
        {
        }

        /// <summary>
        /// Handles a mouse double click.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected override void OnMouseDoubleClick( MouseButtonEventArgs e )
        {
            base.OnMouseDoubleClick( e );
            if( e.OriginalSource == mTextViewer )
            {
                var textOffset = mTextViewer.GetTextOffsetAtPosition( e.GetPosition( mTextViewer ) );
                if( textOffset >= 0 )
                {
                    int wordStart = TextUtilities.FindWordStartAtOffset( mTextViewer.Text, textOffset );
                    int wordEnd = TextUtilities.FindWordEndAtOffset( mTextViewer.Text, textOffset );
                    Select( wordStart, wordEnd - wordStart );
                    e.Handled = true;
                }
            }
            else
            if( e.OriginalSource == mLineNumberViewer )
            {
                SelectAll();
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles mouse move event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );
            if( e.LeftButton == MouseButtonState.Pressed && mIsSelecting )
            {
                var position = e.GetPosition( mTextViewer );
                // Scroll up.
                if( position.Y < 0 )
                {
                    sbVertical.Value = sbVertical.Value - 1;
                }
                else
                // Scroll down.
                if( position.Y > ActualHeight )
                {
                    sbVertical.Value = sbVertical.Value + 1;
                }
            }
        }

        /// <summary>
        /// Handles mouse button up event.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnMouseUp( MouseButtonEventArgs e )
        {
            base.OnMouseUp( e );
            mIsSelecting = false;
            // Release mouse capture from text viewer control.
            if( e.OriginalSource == mTextViewer && e.MouseDevice.Captured == mTextViewer )
                e.MouseDevice.Capture( null );
            // Release mouse capture from line number viewer control.
            if( e.OriginalSource == mLineNumberViewer && e.MouseDevice.Captured == mLineNumberViewer )
                e.MouseDevice.Capture( null );
        }

        /// <summary>
        /// Performs a text hit test.
        /// </summary>
        /// <param name="position">Position for which a hit test should be performed, given in logical units relative to the origin of the text viewer control.</param>
        /// <returns>TextHitInfo structure, or null, if no hit occurred.</returns>
        public TextHitInfo TextHitTest( Point position )
        {
            return mTextViewer.TextHitTest( TranslatePoint( position, mTextViewer ) );
        }

        /// <summary>
        /// Performs a text hit test.
        /// </summary>
        /// <param name="x">Horizontal position for which a hit test should be performed, given in logical units relative to the origin of the text viewer control.</param>
        /// <param name="y">Vertical position for which a hit test should be performed, given in logical units relative to the origin of the text viewer control.</param>
        /// <returns>TextHitInfo structure, or null, if no hit occurred.</returns>
        public TextHitInfo TextHitTest( double x, double y )
        {
            return TextHitTest( new Point( x, y ) );
        }

        /// <summary>
        /// Performs a line hit test.
        /// </summary>
        /// <param name="position">Position for which a hit test should be performed, given in logical units relative to the origin of the text viewer control.</param>
        /// <returns>TextHitInfo structure of the text which has been hit, or null, if no hit occurred.</returns>
        public int LineHitTest( Point position )
        {
            return mLineNumberViewer.LineHitTest( TranslatePoint( position, mLineNumberViewer ) );
        }

        /// <summary>
        /// Performs a line hit test.
        /// </summary>
        /// <param name="x">Horizontal position for which a hit test should be performed, given in logical units relative to the origin of the text viewer control.</param>
        /// <param name="y">Vertical position for which a hit test should be performed, given in logical units relative to the origin of the text viewer control.</param>
        /// <returns>Line index of the line, which was hit, or -1 if no hit occurred.</returns>
        public int LineHitTest( double x, double y )
        {
            return LineHitTest( new Point( x, y ) );
        }

        /// <summary>
        /// Disposes the control.
        /// </summary>
        public void Dispose()
        {
            Dispose( true );
        }

        /// <summary>
        /// Disposes the control.
        /// </summary>
        /// <param name="isDiposing"></param>
        protected virtual void Dispose( bool isDiposing )
        {
            if( isDiposing )
            {
                if( mTextViewer != null )
                {
                    mTextViewer.Dispose();
                    mTextViewer = null;
                }
            }
        }

        private void UserControl_QueryCursor( object sender, QueryCursorEventArgs e )
        {
            if( e.OriginalSource == mTextViewer )
            {
                e.Cursor = Cursors.IBeam;
            }
        }
    }

    /// <summary>
    /// Text viewer visual.
    /// 
    /// This visual is internally used for text rendering by the TextViewerControl.
    /// </summary>
    class TextViewer : FrameworkElement, IDisposable
    {   
        /// <summary>
        /// TextMouseOver routed event.
        /// </summary>
        public static RoutedEvent TextMouseOverEvent = EventManager.RegisterRoutedEvent( "TextMouseOver", RoutingStrategy.Bubble, typeof( TextHitEventHandler ), typeof( TextViewer ) );

        /// <summary>
        /// TextMouseOver event.
        /// 
        /// The TextMouseOver event is raised every time the mouse cursor hovers over a piece of text.
        /// </summary>
        public event TextHitEventHandler TextMouseOver
        {
            add { AddHandler( TextMouseOverEvent, value ); }
            remove { RemoveHandler( TextMouseOverEvent, value ); }
        }

        /// <summary>
        /// TextMouseDown routed event.
        /// </summary>
        public static RoutedEvent TextMouseDownEvent = EventManager.RegisterRoutedEvent( "TextMouseDown", RoutingStrategy.Bubble, typeof( TextHitEventHandler ), typeof( TextViewer ) );

        /// <summary>
        /// TextMouseDown event.
        /// 
        /// The TextMouseDown event is raised every time a mouse button is pressed over a piece of text.
        /// </summary>
        public event TextHitEventHandler TextMouseDown
        {
            add { AddHandler( TextMouseDownEvent, value ); }
            remove { RemoveHandler( TextMouseDownEvent, value ); }
        }

        /// <summary>
        /// TextMouseUp routed event.
        /// </summary>
        public static RoutedEvent TextMouseUpEvent = EventManager.RegisterRoutedEvent( "TextMouseUp", RoutingStrategy.Bubble, typeof( TextHitEventHandler ), typeof( TextViewer ) );

        /// <summary>
        /// TextMouseUp event.
        /// 
        /// The TextMouseUp event is raised every time a mouse button is released over a piece of text.
        /// </summary>
        public event TextHitEventHandler TextMouseUp
        {
            add { AddHandler( TextMouseDownEvent, value ); }
            remove { RemoveHandler( TextMouseDownEvent, value ); }
        }

        /// <summary>
        /// Parent text viewer control.
        /// </summary>
        TextViewerControl mParent;

        /// <summary>
        /// Type face to be used for text rendering.
        /// </summary>
        System.Windows.Media.Typeface mFont;
        /// <summary>
        /// Size of the font to be used for text rendering.
        /// </summary>
        double mFontSize;
        /// <summary>
        /// Line height
        /// </summary>
        double mLineHeight = 1.0;
        /// <summary>
        /// Default text foreground brush to be used for text rendering.
        /// </summary>
        Brush mForeground;

        /// <summary>
        /// Text, which should be rendered.
        /// </summary>
        string mText;
                
        /// <summary>
        /// Offsets within the text, to the start of each line.
        /// </summary>
        int[] mLineOffsets;
        /// <summary>
        /// Formatted text objects for each line of the text.
        /// </summary>
        FormattedText[] mFormattedTextForLine;
        /// <summary>
        /// Array containing each line of the text as a separate string.
        /// </summary>
        string[] mLineText;
        /// <summary>
        /// Array containing boolean flags, defining, whether the line has been foreground highlighted
        /// </summary>
        bool[] mIsForegroundHighlightingValidForLine;
        /// <summary>
        /// Array containing boolean flags, defining, whether the line has been background highlighted
        /// </summary>
        bool[] mIsBackgroundHighlightingValidForLine;
        /// <summary>
        /// Multi line highlighter tokens
        /// </summary>
        IHighlighterToken[] mMultiLineHighlighterTokens;
        /// <summary>
        /// Start offset of multi line highlighter tokens
        /// </summary>
        int[] mMultiLineHighlighterTokensStartOffsets;

        /// <summary>
        /// Array containing lists of highlight geometries for each line.
        /// </summary>
        List<Tuple<Geometry, Brush>>[] mBackgroundHighlightGeometriesForLine;

        /// <summary>
        /// TextFormatter to be used for text measurement and formatting.
        /// </summary>
        TextFormatter mTextFormatter;
        /// <summary>
        /// Text paragraph properties used for text measurement and formatting.
        /// </summary>
        TextParagraphProperties mTextParagraphProperties;
        /// <summary>
        /// Default text run properties used for text measurement and formatting.
        /// </summary>
        TextRunProperties mDefaultTextRunProperties;
        /// <summary>
        /// Number of lines currently visible in viewport.
        /// </summary>
        int mViewportLineCount;
        /// <summary>
        /// Y position of each line currently visible in the viewport.
        /// </summary>
        double[] mViewportLineVerticalPosition;
        /// <summary>
        /// TextLine object for each viewport line
        /// </summary>
        TextLine[] mViewportTextLine;

        /// <summary>
        /// Result of last text hit test. This is used for hit test caching.
        /// </summary>
        TextHitInfo mLastHitTestInfo;
        /// <summary>
        /// Position of last text hit test. This is used for hit test caching.
        /// </summary>
        Point mLastHitTestPosition;

        /// <summary>
        /// Index of the longest line. This is used for computing the maximum text width.
        /// </summary>
        int mLongestLineIndex;
        /// <summary>
        /// Index of the current top line, i.e. the line which is rendered first. Used for vertical line-wise scrolling.
        /// </summary>
        int mTopLine;
        /// <summary>
        /// Horizontal scroll position. This is used for horizontal smooth scrolling.
        /// </summary>
        double mHorizontalScrollPosition;

        /// <summary>
        /// Start offset within the displayed text of the selection.
        /// </summary>
        int mSelectionStart;
        /// <summary>
        /// Length of the selection.
        /// </summary>
        int mSelectionLength;

        /// <summary>
        /// Start position of the selection. (Column and line)
        /// </summary>
        TextPosition mSelectionStartPosition;
        /// <summary>
        /// End position of the selection. (Column and line)
        /// </summary>
        TextPosition mSelectionEndPosition;
        /// <summary>
        /// Stores the selection geometry for each line within the selection range.
        /// </summary>
        Geometry[] mSelectionGeometryForLine;

        /// <summary>
        /// Selection brush.
        /// </summary>
        Brush mSelectionBrush;
        /// <summary>
        /// Selection opacity.
        /// </summary>
        double mSelectionOpacity;

        /// <summary>
        /// Syntax highlighter to be used for syntax highlighting
        /// </summary>
        IHighlighter mHighlighter;

        /// <summary>
        /// Color brush cache.
        /// </summary>
        Dictionary<uint, Brush> mColorBrushes = new Dictionary<uint, Brush>();

        /// <summary>
        /// Collection of foreground color highlight color spans.
        /// </summary>
        ObservableCollection<ColorSpanLayer> mForegroundHighlightColorSpanLayers = new ObservableCollection<ColorSpanLayer>();

        /// <summary>
        /// Collection of background color highlight color spans.
        /// </summary>
        ObservableCollection<ColorSpanLayer> mBackgroundHighlightColorSpanLayers = new ObservableCollection<ColorSpanLayer>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">Parent text viewer control.</param>
        public TextViewer( TextViewerControl parent )
        {
            mParent = parent;

            mTextFormatter = TextFormatter.Create( TextFormattingMode.Ideal );
            mTextParagraphProperties = new TextViewer.TextParagraphProperties( this );
            mDefaultTextRunProperties = new TextViewer.TextRunProperties( this );

            mSelectionBrush = System.Windows.SystemColors.HighlightBrush;
            mSelectionOpacity = 0.4;

            mForegroundHighlightColorSpanLayers.CollectionChanged += OnForegroundHighlightColorSpanLayerCollectionChanged;
            mBackgroundHighlightColorSpanLayers.CollectionChanged += OnBackgroundHighlightColorSpanLayerCollectionChanged;

            AttachToParent();

            ClipToBounds = true;

            UpdateVisualAttributes();
            UpdateFont();
        }

        /// <summary>
        /// Attaches to the parent text viewer control.
        /// 
        /// This method attaches to some of the dependency properties of the TextViewerControl, which affect text rendering.
        /// </summary>
        private void AttachToParent()
        {
            if( mParent != null )
            {
                DependencyPropertyDescriptor.FromProperty( UserControl.FontFamilyProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontSizeProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontStretchProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontWeightProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontStyleProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.ForegroundProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentVisualPropertyChanged );
            }
        }

        /// <summary>
        /// Updates the font to be used for text rendering. By fetching it from the parent TextViewerControl.
        /// </summary>
        private void UpdateFont()
        {
            if( mParent == null )
                return;
            mFont = CreateTypeFace( mParent );
            mFontSize = mParent.FontSize * 96.0 / 72.0;
            mLineHeight = new FormattedText( " gy", System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, mFont, mFontSize, SystemColors.ControlTextBrush ).Height;
            AverageCharWidth = ( new FormattedText( "W", System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, mFont, mFontSize, SystemColors.ControlTextBrush ).MinWidth +
                                 new FormattedText( "X", System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, mFont, mFontSize, SystemColors.ControlTextBrush ).MinWidth +
                                 new FormattedText( "M", System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, mFont, mFontSize, SystemColors.ControlTextBrush ).MinWidth ) / 3.0;
            InvalidateLines();
        }

        /// <summary>
        /// Invalidates all FormattedText objects, which have been created so far for each individual line.
        /// </summary>
        private void InvalidateLines()
        {
            if( mLineOffsets != null )
                mFormattedTextForLine = new FormattedText[mLineOffsets.Length - 1];
        }

        /// <summary>
        /// Invalidates the foreground highlighting for all lines.
        /// </summary>
        private void InvalidateForegroundHighlighting()
        {
            if( mIsForegroundHighlightingValidForLine != null )
            {
                mIsForegroundHighlightingValidForLine = new bool[mLineOffsets.Length - 1];
            }
            mMultiLineHighlighterTokens = null;
            mMultiLineHighlighterTokensStartOffsets = null;
        }

        /// <summary>
        /// Invalidates the background highlighting for all lines.
        /// </summary>
        private void InvalidateBackgroundHighlighting()
        {
            if( mIsBackgroundHighlightingValidForLine != null )
            {
                mIsBackgroundHighlightingValidForLine = new bool[mLineOffsets.Length - 1];
            }
        }

        /// <summary>
        /// Updates the visual attributes, which affect text rendering, by fetching them from the parent TextViewerControl.
        /// </summary>
        private void UpdateVisualAttributes()
        {
            if( mParent == null )
                return;
            mForeground = mParent.Foreground;
        }

        /// <summary>
        /// Handles the change of any font related property of the parent TextViewerControl.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnParentFontPropertyChanged( object sender, EventArgs e )
        {
            UpdateFont();
            InvalidateSelection();
            InvalidateVisual();
        }

        /// <summary>
        /// Handles the change of any visual related property of the parent TextViewerControl.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnParentVisualPropertyChanged( object sender, EventArgs e )
        {
            UpdateVisualAttributes();
            InvalidateVisual();
        }

        /// <summary>
        /// Creates a typeface object based on the font settings of the parent TextViewerControl.
        /// </summary>
        /// <param name="parent">Parent text viewer control.</param>
        /// <returns>Typeface object representing the font of the parent TextViewerControl.</returns>
        private static Typeface CreateTypeFace( TextViewerControl parent )
        {
            return new Typeface( parent.FontFamily, parent.FontStyle, parent.FontWeight, parent.FontStretch );
        }

        /// <summary>
        /// Text, which should be displayed by the TextViewer.
        /// </summary>
        public string Text
        {
            get { return mText; }
            set
            {
                mText = value;
                SelectionStart = 0;
                SelectionLength = 0;
                mLineOffsets = TextUtilities.GetLineOffsets( mText ).ToArray();
                mLineText = new string[mLineOffsets.Length - 1];
                mFormattedTextForLine = new FormattedText[mLineOffsets.Length - 1];
                mIsForegroundHighlightingValidForLine = new bool[mLineOffsets.Length - 1];
                mIsBackgroundHighlightingValidForLine = new bool[mLineOffsets.Length - 1];
                mBackgroundHighlightGeometriesForLine = new List<Tuple<Geometry, Brush>>[mLineOffsets.Length - 1];
                mLongestLineIndex = GetIndexOfLongestLine( mLineOffsets );
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Returns the number of lines in the text, which is displayed.
        /// </summary>
        public int LineCount
        {
            get { return mLineOffsets != null ? mLineOffsets.Length - 1 : 0; }
        }

        /// <summary>
        /// Returns the array of line offsets within the displayed text.
        /// </summary>
        internal int[] LineOffsets
        {
            get { return mLineOffsets; }
        }

        /// <summary>
        /// Returns the maximum horizontal size of the displayed text.
        /// </summary>
        public double HorizontalTextSize
        {
            get
            {
                if( mLongestLineIndex >= 0 )
                {
                    var longestLineText = GetFormattedTextForLine( mLongestLineIndex );
                    return longestLineText != null ? longestLineText.Width : 0.0;
                }
                return 0.0;
            }
        }

        /// <summary>
        /// Returns the vertical size of the current viewport given in text lines.
        /// </summary>
        public double VerticalViewportSize
        {
            get { return Math.Floor( ActualHeight / mLineHeight ); }
        }

        /// <summary>
        /// Returns the horizontal size of the viewport given in logical display units.
        /// </summary>
        public double HorizontalViewportSize
        {
            get { return ActualWidth; }
        }

        /// <summary>
        /// Returns or changes the index of the top most line, which should be displayed. This is used for vertical scrolling.
        /// </summary>
        public int TopLine
        {
            get { return mTopLine; }
            set
            {
                // Clamp value
                value = Math.Min( Math.Max( 0,  value ), LineCount );
                if( mTopLine != value )
                {
                    mTopLine = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Returns or sets the highlighter, which should be used for syntax highlighting
        /// </summary>
        public IHighlighter Highlighter
        {
            get { return mHighlighter; }
            set
            {
                if( mHighlighter != value )
                {
                    mHighlighter = value;
                    mColorBrushes.Clear();
                    InvalidateForegroundHighlighting();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Returns the collection of foreground highlight color span layers.
        /// </summary>
        public ObservableCollection<ColorSpanLayer> ForegroundHighlightColorSpanLayers
        {
            get { return mForegroundHighlightColorSpanLayers; }
        }

        /// <summary>
        /// Returns the collection of background highlight color span layers.
        /// </summary>
        public ObservableCollection<ColorSpanLayer> BackgroundHighlightColorSpanLayers
        {
            get { return mBackgroundHighlightColorSpanLayers; }
        }

        /// <summary>
        /// Returns or sets the horizontal scroll position given in logical display units.
        /// </summary>
        public double HorizontalScrollPosition
        {
            get { return mHorizontalScrollPosition; }
            set
            {
                if( mHorizontalScrollPosition != value )
                {
                    mHorizontalScrollPosition = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Returns the average width of a character. This is used for horizontal scrolling.
        /// </summary>
        public double AverageCharWidth
        {
            private set;
            get;
        }

        /// <summary>
        /// SelectionBrush property.
        /// 
        /// Brush to be used for rendering text selection highlight.
        /// </summary>
        public Brush SelectionBrush
        {
            get { return mSelectionBrush; }
            set
            {
                if( mSelectionBrush != value )
                {
                    mSelectionBrush = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// SelectionOpacity property.
        /// 
        /// Opacity of the selection highlight.
        /// </summary>
        public double SelectionOpacity
        {
            get { return mSelectionOpacity; }
            set
            {
                if( mSelectionOpacity != value )
                {
                    mSelectionOpacity = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// SelectionStart property.
        /// 
        /// Start offset of the text selection, within the displayed text.
        /// </summary>
        public int SelectionStart
        {
            get { return mSelectionStart; }
            set
            {
                if( mSelectionStart != value )
                {
                    mSelectionStart = value;
                    InvalidateSelection();
                }
            }
        }

        /// <summary>
        /// SelectionLength property.
        /// 
        /// Length of the selection.
        /// </summary>
        public int SelectionLength
        {
            get { return mSelectionLength; }
            set
            {
                if( mSelectionLength != value )
                {
                    mSelectionLength = value;
                    InvalidateSelection();
                }
            }
        }

        /// <summary>
        /// SelectedText property.
        /// 
        /// Currently selected text.
        /// </summary>
        public string SelectedText
        {
            get
            {
                if( mText != null )
                {
                    int selectionStart = Math.Max( 0, Math.Min( mSelectionStart, mText.Length ) );
                    int selectionEnd   = Math.Max( 0, Math.Min( mSelectionStart + mSelectionLength, mText.Length ) );
                    return mText.Substring( selectionStart, selectionEnd - selectionStart );
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Selects the specified text range.
        /// </summary>
        /// <param name="selectionStart">Start text offset of the selection range.</param>
        /// <param name="selectionLength">Length of the selection range.</param>
        public void Select( int selectionStart, int selectionLength )
        {
            mSelectionStart = selectionStart;
            mSelectionLength = selectionLength;
            InvalidateSelection();
        }

        /// <summary>
        /// Selects the specified text range.
        /// </summary>
        /// <param name="selectionStartColumn">Column of the selection start.</param>
        /// <param name="selectionStartLine">Line of the selection start.</param>
        /// <param name="selectionEndColumn">Column of the selection end.</param>
        /// <param name="selectionEndLine">Line of the selection end.</param>
        public void Select( int selectionStartColumn, int selectionStartLine, int selectionEndColumn, int selectionEndLine )
        {
            if( mLineOffsets != null )
            {
                // Compute selection start and length.
                int startOffset = mLineOffsets[selectionStartLine] + selectionStartColumn;
                int endOffset = mLineOffsets[selectionEndLine] + selectionEndColumn;

                // Select 
                if( startOffset <= endOffset )
                {
                    Select( startOffset, endOffset - startOffset );
                }
                else
                {
                    Select( endOffset, startOffset - endOffset );
                }
            }
        }

        /// <summary>
        /// Selects the specified text range.
        /// </summary>
        /// <param name="selectionStartColumn">Column of the selection start.</param>
        /// <param name="selectionStartLine">Line of the selection start.</param>
        /// <param name="selectionLength">Length of the selection range.</param>
        public void Select( int selectionStartColumn, int selectionStartLine, int selectionLength )
        {
            if( mLineOffsets != null )
            {
                // Compute selection start and length.
                int startOffset = mLineOffsets[selectionStartLine] + selectionStartColumn;
                Select( startOffset, selectionLength );
            }
        }

        /// <summary>
        /// Invalidates the current selection.
        /// </summary>
        private void InvalidateSelection()
        {
            mSelectionStartPosition = TextUtilities.OffsetToColumnAndLineIndex( mSelectionStart, mLineOffsets );
            mSelectionEndPosition = TextUtilities.OffsetToColumnAndLineIndex( mSelectionStart + mSelectionLength, mLineOffsets );
            mSelectionGeometryForLine = ( mSelectionLength > 0 ) ? new Geometry[ mSelectionEndPosition.Line - mSelectionStartPosition.Line + 1 ] : null;
            InvalidateVisual();
        }

        /// <summary>
        /// Determines the index of the longest line.
        /// </summary>
        /// <param name="lineOffsets">Array with line offsets within the text, which is displayed.</param>
        /// <returns>Index of the longest line, or -1 if none could be found.</returns>
        private static int GetIndexOfLongestLine( int[] lineOffsets )        
        {
            return GetIndexOfLongestLine( lineOffsets, 0, lineOffsets.Length - 1 );
        }

        /// <summary>
        /// Determines the index of the longest line.
        /// </summary>
        /// <param name="lineOffsets">Array with line offsets within the text, which is displayed.</param>
        /// <param name="startLine">Line at which search should start.</param>
        /// <param name="endLine">Line at which search should end. This line is not considered.</param>
        /// <returns>Index of the longest line, or -1 if none could be found.</returns>
        private static int GetIndexOfLongestLine( int[] lineOffsets, int startLine, int endLine )
        {
            int longestLineIndex = -1;
            int longestLineLength = -1;

            for( int i = startLine ; i < endLine ; ++i )
            {
                int lineLength = lineOffsets[i + 1] - lineOffsets[i];
                if( lineLength > longestLineLength )
                {
                    longestLineLength = lineLength;
                    longestLineIndex = i;
                }
            }

            return longestLineIndex;
        }

        /// <summary>
        /// Returns the size of the specified line in characters.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which the size should be determined.</param>
        /// <returns>Line size in characters.</returns>
        private int GetLineSize( int lineIndex )
        {
            return mLineOffsets[lineIndex + 1] - mLineOffsets[lineIndex];
        }

        /// <summary>
        /// Returns the text of the specified line as a string.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which the text should be returned.</param>
        /// <returns>Text of the specified line, as a string.</returns>
        private string GetLineText( int lineIndex )
        {
            if( lineIndex >= mLineText.Length )
                return null;
            if( mLineText[lineIndex] == null )
                mLineText[lineIndex] = mText.Substring( mLineOffsets[lineIndex], mLineOffsets[lineIndex + 1] - mLineOffsets[lineIndex] );
            return mLineText[lineIndex];
        }

        /// <summary>
        /// Returns the formatted text object for the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which a formatted text should be returned.</param>
        /// <returns>FormattedText object of the specified line.</returns>
        private FormattedText GetFormattedTextForLine( int lineIndex )
        {
            if( mFormattedTextForLine == null )
                return null;

            if( mFormattedTextForLine[lineIndex] == null )
            {
                mFormattedTextForLine[lineIndex] = new FormattedText( GetLineText( lineIndex ), System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, mFont, mFontSize, mForeground );
            }
            return mFormattedTextForLine[lineIndex];
        }

        /// <summary>
        /// Returns the highlighter token located at the given text position.
        /// </summary>
        /// <param name="textOffset">Text offset for which a highlighter token should be found.</param>
        /// <returns>Highlighter token crossing the specified text position, or null, if no highlighter token could be found.</returns>
        private IHighlighterToken GetHighlighterTokenAtTextOffset( int textOffset, out string highlighterTokenText )
        {
            highlighterTokenText = null;
            if( mHighlighter != null )
            {
                TextPosition textPosition = TextUtilities.OffsetToColumnAndLineIndex( textOffset, mLineOffsets );
                if( textPosition != TextPosition.Invalid && textPosition.Line < LineCount )
                {
                    textOffset -= mLineOffsets[textPosition.Line];
                    foreach( var highlighterToken in mHighlighter.Highlight( GetLineText( textPosition.Line ) ) )
                    {
                        if( highlighterToken.StartPosition <= textOffset && textOffset < highlighterToken.EndPosition )
                        {
                            highlighterTokenText = GetLineText( textPosition.Line ).Substring( highlighterToken.StartPosition, highlighterToken.EndPosition - highlighterToken.StartPosition );
                            return new HighlighterToken( highlighterToken.Color, highlighterToken.Type, highlighterToken.StartPosition + mLineOffsets[ textPosition.Line ], highlighterToken.EndPosition + mLineOffsets[ textPosition.Line ] );
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Updates the foreground highlighting for the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of line, for which the foreground highlighting should be updated.</param>
        /// <param name="activeMultiLineBlockHighlighterToken">Multi line block highlighter token, which is currently active</param>
        /// <param name="force">Flag indicating, whether the update should be performed, even if the highlighting is valid.</param>
        private void UpdateForegroundHighlighting( int lineIndex, IHighlighterToken activeMultiLineBlockHighlighterToken, bool force = false )
        {
            if( mIsForegroundHighlightingValidForLine == null )
                return;
            if( mIsForegroundHighlightingValidForLine[lineIndex] && !force )
                return;

            var formattedTextOfLine = GetFormattedTextForLine( lineIndex );
            if( formattedTextOfLine == null )
                return;

            int lineOffset = mLineOffsets[ lineIndex ];
            int lineLength = GetLineSize( lineIndex );

            formattedTextOfLine.SetForegroundBrush( mForeground );

            // Highlight using syntax highlighter
            if( mHighlighter != null )
            {
                int blockStartPosition = 0;
                int blockEndPosition = 0;

                // Highlight using active multiline highlighter block token
                if( activeMultiLineBlockHighlighterToken != null )
                {
                    blockStartPosition = activeMultiLineBlockHighlighterToken.StartPosition - lineOffset;
                    if( blockStartPosition < 0 )
                        blockStartPosition = 0;
                    blockEndPosition = activeMultiLineBlockHighlighterToken.EndPosition - lineOffset;
                    if( blockEndPosition >= 0 )
                        formattedTextOfLine.SetForegroundBrush( GetColorBrush( activeMultiLineBlockHighlighterToken.Color ), blockStartPosition, Math.Min( blockEndPosition - blockStartPosition, lineLength - blockStartPosition ) );
                }

                foreach( var highlighterToken in mHighlighter.Highlight( GetLineText( lineIndex ), activeMultiLineBlockHighlighterToken != null ? new HighlighterToken( activeMultiLineBlockHighlighterToken.Color, activeMultiLineBlockHighlighterToken.Type, blockStartPosition, blockEndPosition ): null ) )
                    formattedTextOfLine.SetForegroundBrush( GetColorBrush( highlighterToken.Color ), highlighterToken.StartPosition, highlighterToken.EndPosition - highlighterToken.StartPosition );
            }

            // Highlight using foreground highlight layers.
            foreach( var foregroundHighlightLayer in mForegroundHighlightColorSpanLayers )
            {
                if( foregroundHighlightLayer != null )
                {
                    var colorSpans = foregroundHighlightLayer.GetColorSpansForLine( lineIndex );
                    if( colorSpans != null )
                    {
                        foreach( var colorSpan in colorSpans )
                        {
                            var color = colorSpan.Color ?? foregroundHighlightLayer.DefaultBrush;
                            formattedTextOfLine.SetForegroundBrush( color, colorSpan.Start, colorSpan.End - colorSpan.Start );
                        }
                    }
                }
            }

            mIsForegroundHighlightingValidForLine[lineIndex] = true;
        }

        /// <summary>
        /// Updates the background highlighting for the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of line, for which the background highlighting should be updated.</param>
        /// <param name="force">Flag indicating, whether the update should be performed, even if the highlighting is valid.</param>
        private void UpdateBackgroundHighlighting( int lineIndex, bool force = false )
        {
            if( mIsBackgroundHighlightingValidForLine == null )
                return;
            if( mIsBackgroundHighlightingValidForLine[lineIndex] && !force )
                return;

            List<Tuple<Geometry, Brush>> highlightGeometryList = null;
            var formattedTextOfLine = GetFormattedTextForLine( lineIndex );

            // Create highlight geometries by applying highlights from background highlight layers.
            foreach( var backgroundHighlightLayer in mBackgroundHighlightColorSpanLayers )
            {
                var colorSpans = backgroundHighlightLayer.GetColorSpansForLine( lineIndex );
                if( colorSpans != null )
                {
                    if( highlightGeometryList == null )
                        highlightGeometryList = new List<Tuple<Geometry, Brush>>();
                    foreach( var colorSpan in colorSpans )
                        highlightGeometryList.Add( new Tuple<Geometry, Brush>( formattedTextOfLine.BuildHighlightGeometry( new Point(0,0), colorSpan.Start, colorSpan.End - colorSpan.Start ), colorSpan.Color ?? backgroundHighlightLayer.DefaultBrush ) );
                }            
            }

            mBackgroundHighlightGeometriesForLine[lineIndex] = highlightGeometryList;
            mIsBackgroundHighlightingValidForLine[lineIndex] = true;
        }

        /// <summary>
        /// Returns a brush corresponding to the argb encoded color.
        /// </summary>
        /// <param name="color">ARGB color stored in an int.</param>
        /// <returns>SolidColorBrush corresponding to the specified color.</returns>
        private Brush GetColorBrush( uint color )
        {
            Brush colorBrush = null;
            if( !mColorBrushes.TryGetValue( color, out colorBrush ) )
            {
                colorBrush = new System.Windows.Media.SolidColorBrush( System.Windows.Media.Color.FromArgb( (byte) ( color >> 24 ), (byte) ( color >> 16 ), (byte) ( color >> 8 ), (byte) ( color ) ) );
                mColorBrushes.Add( color, colorBrush );
            }
            return colorBrush;            
        }

        /// <summary>
        /// Returns the selection geometry for the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which a selection geometry should be returned.</param>
        /// <returns>Selection geometry for the specified line, or null, if the line does not contain any selection.</returns>
        private Geometry GetSelectionGeometryForLine( int lineIndex )
        {
            if( mSelectionGeometryForLine == null )
                return null;

            if( lineIndex >= mSelectionStartPosition.Line && lineIndex <= mSelectionEndPosition.Line )
            {
                // Map line index onto geometry index.
                int geometryIndex = lineIndex - mSelectionStartPosition.Line;
                // Selection geometry not created yet?
                if( mSelectionGeometryForLine[geometryIndex] == null )
                {
                    // Create selection geometry, by using FormattedText
                    var formattedTextForLine = GetFormattedTextForLine( lineIndex );
                    if( formattedTextForLine != null )
                    {
                        // Compute start index and size of the highlight geometry.
                        int startIndex = 0;
                        int count = GetLineText( lineIndex ).Length;
                        
                        // Are we at the start of the selection?
                        if( lineIndex == mSelectionStartPosition.Line )
                        {
                            // Adjust start index and count.
                            startIndex = mSelectionStartPosition.Column;
                            count -= mSelectionStartPosition.Column;
                        }
                        // Are we at the end of the selection.
                        if( lineIndex == mSelectionEndPosition.Line )
                        {
                            count = mSelectionEndPosition.Column - startIndex;
                        }
                        mSelectionGeometryForLine[geometryIndex] = formattedTextForLine.BuildHighlightGeometry( new Point( 0, 0 ), startIndex, count );
                    }
                }
                return mSelectionGeometryForLine[geometryIndex];
            }

            return null;
        }

        /// <summary>
        /// Returns the horizontal position of the specified text offset within the displayed text.
        /// </summary>
        /// <param name="textOffset"></param>
        /// <returns>Horizontal position in logical units of the specified text offset.</returns>
        internal double GetHorizontalPositionOfTextOffset( int textOffset )
        {
            if( mLineOffsets != null )
            {
                return GetHorizontalPositionOfColumnInLine( TextUtilities.OffsetToColumnAndLineIndex( textOffset, mLineOffsets ) );
            }
            return 0.0;
        }

        /// <summary>
        /// Returns the horizontal position of the specified column in the specified line within the displayed text.
        /// </summary>
        /// <param name="textPosition">Tuple containing the column and the line.</param>
        /// <returns>Horizontal position in logical units of the specified column in the specified line.</returns>
        internal double GetHorizontalPositionOfColumnInLine( TextPosition textPosition )
        {
            if( textPosition.Line >= 0 )
            {
                var lineFormattedText = GetFormattedTextForLine( textPosition.Line );
                var highlightGeometry = lineFormattedText.BuildHighlightGeometry( new Point( 0.0, 0.0 ), 0, textPosition.Column );
                return highlightGeometry != null ? highlightGeometry.Bounds.Right : 0.0;
            }
            return 0.0;
        }

        /// <summary>
        /// Returns the horizontal extents of the specified text range including trailing whitespace.
        /// </summary>
        /// <param name="rangeStartOffset">Start offset of the range within the displayed text.</param>
        /// <param name="rangeEndOffset">End offset of the range within the displayed text.</param>
        /// <returns>Tuple containing the minimum horizontal position and the maximum horizontal position of the text range.</returns>
        internal Tuple<double, double> GetHorizontalExtentsOfTextRange( int rangeStartOffset, int rangeEndOffset )
        {
            return GetHorizontalExtentsOfTextRange( TextUtilities.OffsetToColumnAndLineIndex( rangeStartOffset, mLineOffsets ), TextUtilities.OffsetToColumnAndLineIndex( rangeEndOffset, mLineOffsets ) );
        }

        /// <summary>
        /// Returns the horizontal extents of the specified text range including trailing whitespace.
        /// </summary>
        /// <param name="rangeStart">Start column and line index of the range ( zero based ).</param>
        /// <param name="rangeEnd">End column and line index of the range ( zero based ).</param>
        /// <returns>Tuple containing the minimum horizontal position and the maximum horizontal position of the text range.</returns>
        internal Tuple<double, double> GetHorizontalExtentsOfTextRange( TextPosition rangeStart, TextPosition rangeEnd )
        {
            double maxX = 0.0;
            double minX = Double.PositiveInfinity;

            double startHorizontalPosition = GetHorizontalPositionOfColumnInLine( rangeStart );

            if( startHorizontalPosition > maxX )
                maxX = startHorizontalPosition;
            if( startHorizontalPosition < minX )
                minX = startHorizontalPosition;

            double endHorizontalPosition = GetHorizontalPositionOfColumnInLine( rangeEnd );

            if( endHorizontalPosition > maxX )
                maxX = endHorizontalPosition;
            if( endHorizontalPosition < minX )
                minX = endHorizontalPosition;

            if( ( rangeEnd.Line - rangeStart.Line ) > 1 )
            {
                int longestLineIndex = GetIndexOfLongestLine( mLineOffsets, rangeStart.Line + 1, rangeEnd.Line );
                if( longestLineIndex >= 0 )
                {
                    double longestLineWidth = GetHorizontalPositionOfColumnInLine( new Tuple<int,int>( GetLineSize( longestLineIndex ), longestLineIndex ) );
                    minX = 0.0;
                    if( longestLineWidth > maxX )
                        maxX = longestLineWidth;
                }
            }

            return new Tuple<double, double>( minX, maxX );
        }

        /// <summary>
        /// Returns the zero-based index of the line at the specified vertical position.
        /// </summary>
        /// <param name="verticalPosition">Vertical position relative to the top of the text viewer in logical units.</param>
        /// <returns>Index of the line at the specified position, or -1 if line index cannot be retrieved.</returns>
        internal int GetLineIndexAtVerticalPosition( double verticalPosition )
        {
            if( mViewportLineVerticalPosition != null )
            {
                var lineIndex = Array.BinarySearch( mViewportLineVerticalPosition, 0, mViewportLineCount, verticalPosition );
                if( lineIndex < 0 )
                    lineIndex = -lineIndex - 2;
                return lineIndex + mTopLine;
            }
            return -1;
        }

        /// <summary>
        /// Returns the text offset of the specified column and line.
        /// </summary>
        /// <param name="columnAndLineIndex">TextPosition object containing the column and the line index.</param>
        /// <returns>Offset within the displayed text corresponding to the specified text position, or -1 if the text position is invalid.</returns>
        internal int GetTextOffsetAtLineAndColumn( TextPosition columnAndLineIndex )
        {
            return TextUtilities.ColumnAndLineIndexToOffset( columnAndLineIndex, mLineOffsets );
        }

        /// <summary>
        /// Returns the text offset of the specified column and line.
        /// </summary>
        /// <param name="columnIndex">Column index ( zero based ) .</param>
        /// <param name="lineIndex">Column index ( zero based ) .</param>
        /// <returns>Offset within the displayed text corresponding to the specified text position, or -1 if the text position is invalid.</returns>
        internal int GetTextOffsetAtLineAndColumn( int columnIndex, int lineIndex )
        {
            return TextUtilities.ColumnAndLineIndexToOffset( new TextPosition( columnIndex, lineIndex ), mLineOffsets );
        }

        /// <summary>
        /// Returns the column and line index at the specified mouse position.
        /// </summary>
        /// <param name="position">Position relative to the text viewer given in logical units.</param>
        /// <returns>Column and line index of the text at the specified mouse position, or TextPosition.Invalid.</returns>
        internal TextPosition GetColumnAndLineIndexAtPosition( Point position )
        {
            return GetColumnAndLineIndexAtPosition( position.X, position.Y );
        }

        /// <summary>
        /// Returns the column and line index at the specified mouse position.
        /// </summary>
        /// <param name="x">Horizontal position relative to the text viewer given in logical units.</param>
        /// <param name="y">Vertical position relative to the text viewer given in logical units.</param>
        /// <returns>Column and line index of the text at the specified position, or TextPosition.Invalid.</returns>
        internal TextPosition GetColumnAndLineIndexAtPosition( double x, double y )
        {
            int lineIndex = GetLineIndexAtVerticalPosition( y );
            if( lineIndex >= mTopLine )
            {
                int viewportLineIndex = lineIndex - mTopLine;
                var textLine = mViewportTextLine[viewportLineIndex];
                var horizontalPosition = x + mHorizontalScrollPosition + 2.0;
                if( horizontalPosition > textLine.Width )
                    return new TextPosition( textLine.Length, lineIndex );
                var characterHit = textLine.GetCharacterHitFromDistance( horizontalPosition );
                return new TextPosition( characterHit.FirstCharacterIndex, lineIndex );
            }
            return TextPosition.Invalid;
        }

        /// <summary>
        /// Returns the offset within the displayed text at the specified position.
        /// </summary>
        /// <param name="position">Position relative to the text viewer given in logical units.</param>
        /// <returns>Text offset of the specified position or -1, if no text is available at the specified position.</returns>
        internal int GetTextOffsetAtPosition( Point position  )
        {
            return GetTextOffsetAtPosition( position.X, position.Y );
        }

        /// <summary>
        /// Returns the offset within the displayed text at the specified position.
        /// </summary>
        /// <param name="x">Horizontal position relative to the text viewer given in logical units.</param>
        /// <param name="y">Vertical position relative to the text viewer given in logical units.</param>
        /// <returns>Text offset of the specified position or -1, if no text is available at the specified position.</returns>
        internal int GetTextOffsetAtPosition( double x, double y )
        {
            var columnAndLine = GetColumnAndLineIndexAtPosition( x, y );
            if( columnAndLine.Line >= 0 )
                return mLineOffsets[columnAndLine.Line] + columnAndLine.Column;
            return -1;
        }

        /// <summary>
        /// Handles the change of the foreground color span layer collection.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnBackgroundHighlightColorSpanLayerCollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
        {
            if( e.NewItems != null )
            {
                foreach( ColorSpanLayer newLayer in e.NewItems )
                    newLayer.Changed += OnBackgroundHighlightColorSpanLayerChanged;
            }
            if( e.OldItems != null )
            {
                foreach( ColorSpanLayer oldLayer in e.OldItems )
                    oldLayer.Changed -= OnBackgroundHighlightColorSpanLayerChanged;
            }
            InvalidateBackgroundHighlighting();
            InvalidateVisual();
        }

        /// <summary>
        /// Handles the change of the background color span layer collection.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnForegroundHighlightColorSpanLayerCollectionChanged( object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e )
        {
            if( e.NewItems != null )
            {
                foreach( ColorSpanLayer newLayer in e.NewItems )
                    newLayer.Changed += OnForegroundHighlightColorSpanLayerChanged;
            }
            if( e.OldItems != null )
            {
                foreach( ColorSpanLayer oldLayer in e.OldItems )
                    oldLayer.Changed -= OnForegroundHighlightColorSpanLayerChanged;
            }
            InvalidateForegroundHighlighting();
            InvalidateVisual();
        }

        /// <summary>
        /// Handles the change within a single background highlight color span layer.
        /// </summary>
        /// <param name="layer">Layer, which has changed.</param>
        /// <param name="lineIndex">Index of the line, which has changed.</param>
        private void OnBackgroundHighlightColorSpanLayerChanged( ColorSpanLayer layer, int lineIndex )
        {
            if( lineIndex >= 0 )
            {
                mIsBackgroundHighlightingValidForLine[ lineIndex ] = false;
                if( lineIndex > mTopLine && lineIndex < mTopLine + VerticalViewportSize )
                    InvalidateVisual();
            }
            else
            {
                InvalidateBackgroundHighlighting();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="layer"></param>
        /// <param name="lineIndex"></param>
        private void OnForegroundHighlightColorSpanLayerChanged( ColorSpanLayer layer, int lineIndex )
        {
            if( lineIndex >= 0 )
            {
                mIsForegroundHighlightingValidForLine[lineIndex] = false;
                if( lineIndex > mTopLine && lineIndex < mTopLine + VerticalViewportSize )
                    InvalidateVisual();
            }
            else
            {
                InvalidateForegroundHighlighting();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// Performs a text hit test at the specified position.
        /// </summary>
        /// <param name="position">Position at which the text should be hit.</param>
        /// <returns>TextHitEventArgs describing the hit test results, or null, if no text has been hit.</returns>
        public TextHitInfo TextHitTest( Point position )
        {
            // Hit test cache, if valid.
            if( !double.IsNaN( mLastHitTestPosition.X ) && !double.IsNaN( mLastHitTestPosition.Y ) )
            {
                if( ( Math.Abs( position.X - mLastHitTestPosition.X ) <= double.Epsilon ) &&
                    ( Math.Abs( position.Y - mLastHitTestPosition.Y ) <= double.Epsilon ) )
                {
                    return mLastHitTestInfo;
                }
            }

            // Store last hit test position for caching.
            mLastHitTestPosition = position;

            var textPosition = GetColumnAndLineIndexAtPosition( position );
            if( textPosition != TextPosition.Invalid && textPosition.Line < LineCount )
            {
                var textOffset = TextUtilities.ColumnAndLineIndexToOffset( textPosition, mLineOffsets );
                string highlighterTokenText;
                var highlighterToken = GetHighlighterTokenAtTextOffset( textOffset, out highlighterTokenText );
                // Cache hit test result.
                mLastHitTestInfo = new TextHitInfo( textPosition, textOffset, highlighterToken, highlighterTokenText );
                return mLastHitTestInfo;
            }

            // Cache hit test result.
            mLastHitTestInfo = null;
            return mLastHitTestInfo;
        }

        /// <summary>
        /// Handles the mouse down event.
        /// </summary>
        /// <param name="e>Event args.</param>
        protected override void OnMouseDown( MouseButtonEventArgs e )
        {
            base.OnMouseDown( e );

            // Get text hit by the mouse cursor.
            var mousePosition = e.GetPosition( this );
            var textHitInfo = TextHitTest( mousePosition );
            if( textHitInfo != null )
                RaiseEvent( new TextHitEventArgs( TextMouseDownEvent, this, textHitInfo, e ) );
        }

        /// <summary>
        /// Handles the mouse move event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );

            // Get text hit by the mouse cursor.
            var mousePosition = e.GetPosition( this );
            var textHitInfo = TextHitTest( mousePosition );
            if( textHitInfo != null )
                RaiseEvent( new TextHitEventArgs( TextMouseOverEvent, this, textHitInfo, e ) );
        }

        /// <summary>
        /// Handles the mouse up event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected override void OnMouseUp( MouseButtonEventArgs e )
        {
            base.OnMouseUp( e );

            // Get text hit by the mouse cursor.
            var mousePosition = e.GetPosition( this );
            var textHitInfo = TextHitTest( mousePosition );
            if( textHitInfo != null )
                RaiseEvent( new TextHitEventArgs( TextMouseUpEvent, this, textHitInfo, e ) );
        }
      
        /// <summary>
        /// Handles rendering.
        /// </summary>
        /// <param name="drawingContext">Drawing context, to be used for rendering.</param>
        protected override void OnRender( DrawingContext drawingContext )        
        {
            mLastHitTestInfo = null;
            mLastHitTestPosition.X = double.NaN;
            mLastHitTestPosition.Y = double.NaN;

            // Prefetch values.
            double actualWidth = ActualWidth;
            double actualHeight = ActualHeight;
            double horizontalTextSize = HorizontalTextSize;
            int lineCount = LineCount;

            // Update multi line highlighter blocks
            UpdateMultiLineHighlighterBlocks();

            // Prepare viewport related structures
            int numberOfVisibleLines = (int) Math.Ceiling( actualHeight / mLineHeight ) + 1;
            if( mViewportLineVerticalPosition == null || numberOfVisibleLines > mViewportLineVerticalPosition.Length )
                mViewportLineVerticalPosition = new double[numberOfVisibleLines];
            if( mViewportTextLine == null || numberOfVisibleLines > mViewportTextLine.Length )
            {
                DisposeViewportTextLines();
                mViewportTextLine = new TextLine[numberOfVisibleLines];
            }

            // Paint background
            drawingContext.DrawRectangle( mParent.Background, null, new Rect( 0, 0, actualWidth, actualHeight ) );

            // Check, if anything must be rendered.
            mViewportLineCount = 0;
            if( mTopLine < lineCount )
            {
                Point origin = new Point( -mHorizontalScrollPosition, 0.0 );
                // Render lines
                for( int lineIndex = mTopLine ; lineIndex < lineCount ; ++lineIndex )
                {
                    mViewportLineVerticalPosition[mViewportLineCount] = origin.Y;

                    // Skip, if outside viewport.
                    if( origin.Y > actualHeight )
                        break;

                    // Compute metrics for the line
                    if( mViewportTextLine[mViewportLineCount] != null )
                        mViewportTextLine[mViewportLineCount].Dispose();
                    mViewportTextLine[mViewportLineCount] = mTextFormatter.FormatLine( new TextSource( this, lineIndex ), 0, 0.0, mTextParagraphProperties, null ); 

                    // Update foreground highlighting for the current line
                    UpdateForegroundHighlighting( lineIndex, GetActiveMultiLineHighlighterBlockFromTextOffset( mLineOffsets[lineIndex] ) );
                    // Update background highlighting for the current line
                    UpdateBackgroundHighlighting( lineIndex );

                    // Get and render background highlight geometries of the current line
                    if( mBackgroundHighlightGeometriesForLine[ lineIndex ] != null )
                    {
                        drawingContext.PushTransform( new TranslateTransform( -mHorizontalScrollPosition, origin.Y ) );
                        foreach( var highlightGeometry in mBackgroundHighlightGeometriesForLine[lineIndex] )
                            drawingContext.DrawGeometry( highlightGeometry.Item2, null, highlightGeometry.Item1 );
                        drawingContext.Pop();
                    }

                    // Get and render FormattedText object of the current line
                    var lineText = GetFormattedTextForLine( lineIndex );
                    drawingContext.DrawText( lineText, origin );

                    // Get and render selection geometry of the current line
                    var lineSelection = GetSelectionGeometryForLine( lineIndex );
                    if( lineSelection != null )
                    {
                        drawingContext.PushOpacity( mSelectionOpacity );
                        drawingContext.PushTransform( new TranslateTransform( -mHorizontalScrollPosition, origin.Y ) );
                        drawingContext.DrawGeometry( mSelectionBrush, null, lineSelection );
                        drawingContext.Pop();
                        drawingContext.Pop();
                    }
                    // Update rendering position
                    origin.Y += lineText.Height;
                    ++mViewportLineCount;
                }
            }
        }

        /// <summary>
        /// Returns the active multi line highlighter token for the specified text offset
        /// </summary>
        /// <param name="textOffset"></param>
        /// <returns></returns>
        private IHighlighterToken GetActiveMultiLineHighlighterBlockFromTextOffset( int textOffset )
        {
            if( mMultiLineHighlighterTokensStartOffsets != null && mMultiLineHighlighterTokens.Length > 0 )
            {
                int blockIndex = Array.BinarySearch( mMultiLineHighlighterTokensStartOffsets, textOffset );
                if( blockIndex < 0 )
                    blockIndex = -blockIndex - 2;
                if( blockIndex < 0 )
                    blockIndex = mMultiLineHighlighterTokens.Length - 1;
                IHighlighterToken token;
                token = mMultiLineHighlighterTokens[blockIndex];
                if( token.StartPosition <= textOffset && textOffset < token.EndPosition )
                    return token;
            }
            return null;                
        }

        /// <summary>
        /// Checks, whether the token spans multiple lines.
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private bool IsMultiLineBlock( IHighlighterToken token )
        {
            if( token != null )
            {
                TextPosition tokenStart = TextUtilities.OffsetToColumnAndLineIndex( token.StartPosition, mLineOffsets );
                TextPosition tokenEnd = TextUtilities.OffsetToColumnAndLineIndex( token.EndPosition, mLineOffsets );
                return tokenStart.Line != tokenEnd.Line;
            }
            return false;
        }

        /// <summary>
        /// Updates the list of multi line highlighter blocks, if it has been invalidated.
        /// </summary>
        private void UpdateMultiLineHighlighterBlocks()
        {
            if( mHighlighter != null && mMultiLineHighlighterTokens == null )
            {
                mMultiLineHighlighterTokens = mHighlighter.HighlightMultiLineBlocks( mText ).Where( IsMultiLineBlock ).ToArray();
                mMultiLineHighlighterTokensStartOffsets = mMultiLineHighlighterTokens.Select( token => token.StartPosition ).ToArray();
            }
        }

        public void Dispose()
        {
            Dispose( true );
        }

        protected virtual void Dispose( bool isDisposing )
        {
            if( isDisposing )
            {
                DisposeViewportTextLines();
            }
        }

        /// <summary>
        /// Disposes the viewport TextLine objects.
        /// </summary>
        private void DisposeViewportTextLines()
        {
            if( mViewportTextLine != null )
            {
                for( int i = 0 ; i < mViewportTextLine.Length ; ++i )
                {
                    if( mViewportTextLine[i] != null )
                    {
                        mViewportTextLine[i].Dispose();
                        mViewportTextLine[i] = null;
                    }
                }
            }
        }

        /// <summary>
        /// TextSource implementation used for providing text information to the TextFormatter.
        /// </summary>
        private class TextSource : System.Windows.Media.TextFormatting.TextSource
        {
            private TextViewer mParent;
            private TextRun mTextRun;

            internal TextSource( TextViewer parent, int lineIndex )
            {
                mParent = parent;
                if( parent.mLineOffsets[lineIndex] < parent.mText.Length )
                    mTextRun = new TextCharacters( parent.mText, parent.mLineOffsets[lineIndex], parent.GetLineSize( lineIndex ), mParent.mDefaultTextRunProperties );
            }

            public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText( int textSourceCharacterIndexLimit )
            {
                return null;
            }

            public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex( int textSourceCharacterIndex )
            {
                return textSourceCharacterIndex;
            }

            public override System.Windows.Media.TextFormatting.TextRun GetTextRun( int textSourceCharacterIndex )
            {
                if( mTextRun == null || textSourceCharacterIndex >= mTextRun.Length )
                    return new TextEndOfParagraph( 1 );
                return mTextRun;
            }
        }

        /// <summary>
        /// TextParagraphProperties implementation used for providing paragraph information to the TextFormatter.
        /// </summary>
        private class TextParagraphProperties : System.Windows.Media.TextFormatting.TextParagraphProperties
        {
            private TextViewer mParent;

            internal TextParagraphProperties( TextViewer parent )
            {
                mParent = parent;
            }

            public override System.Windows.Media.TextFormatting.TextRunProperties DefaultTextRunProperties
            {
                get
                {
                    return mParent.mDefaultTextRunProperties;
                }
            }

            public override bool FirstLineInParagraph
            {
                get { return true; }
            }

            public override FlowDirection FlowDirection
            {
                get { return FlowDirection.LeftToRight; }
            }

            public override double Indent
            {
                get { return 0; }
            }

            public override double LineHeight
            {
                get { return double.NaN; }
            }

            public override TextAlignment TextAlignment
            {
                get { return TextAlignment.Left; }
            }

            public override TextMarkerProperties TextMarkerProperties
            {
                get { return null; }
            }

            public override TextWrapping TextWrapping
            {
                get { return TextWrapping.NoWrap; }
            }
        }

        /// <summary>
        /// TextRunProperties implementation used for providing default TextRun properties to the TextFormatter.
        /// </summary>
        private class TextRunProperties : System.Windows.Media.TextFormatting.TextRunProperties
        {
            private TextViewer mParent;

            internal TextRunProperties( TextViewer parent )
            {
                mParent = parent;
            }

            public override Brush BackgroundBrush
            {
                get { return null; }
            }

            public override System.Globalization.CultureInfo CultureInfo
            {
                get { return System.Globalization.CultureInfo.CurrentUICulture; }
            }

            public override double FontHintingEmSize
            {
                get { return mParent.mFontSize; }
            }

            public override double FontRenderingEmSize
            {
                get { return mParent.mFontSize; }
            }

            public override Brush ForegroundBrush
            {
                get { return mParent.mForeground; }
            }

            public override TextDecorationCollection TextDecorations
            {
                get { return null; }
            }

            public override TextEffectCollection TextEffects
            {
                get { return null; }
            }

            public override Typeface Typeface
            {
                get { return mParent.mFont; }
            }
        }
    }

    /// <summary>
    /// Highlight layer class.
    /// </summary>
    public class HighlightLayer : Freezable
    {
        /// <summary>
        /// HighlightBrush dependency property.
        /// 
        /// This dependency property stores the HighlightBrush, which should be used for highlighting elements of the layer.
        /// </summary>
        public static readonly DependencyProperty HighlightBrushProperty = DependencyProperty.Register( "HighlightBrush", typeof( Brush ), typeof( HighlightLayer ), new FrameworkPropertyMetadata( null, OnHighlightBrushPropertyChanged ) );

        /// <summary>
        /// Handles change of the HighlightBrush dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnHighlightBrushPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            HighlightLayer control = sender as HighlightLayer;
            if( control != null )
                control.OnHighlightBrushChanged( eventArgs.NewValue as Brush );
        }

        /// <summary>
        /// Handles change of the text property.
        /// </summary>
        /// <param name="text">New text.</param>
        protected virtual void OnHighlightBrushChanged( Brush highlightBrush )
        {
        }

        /// <summary>
        /// HighlightBrush property.
        /// 
        /// HighlightBrush, which should be used for highlighting elements in the layer.
        /// </summary>
        public Brush HighlightBrush
        {
            get { return GetValue( HighlightBrushProperty ) as Brush; }
            set { SetValue( HighlightBrushProperty, value ); }
        }

        /// <summary>
        /// Ranges dependency property.
        /// 
        /// This dependency property stores the Ranges enumerable, defining the elements, which should be highlighted.
        /// </summary>
        public static readonly DependencyProperty RangesProperty = DependencyProperty.Register( "Ranges", typeof( IEnumerable<Tuple<int, int, int, int>> ), typeof( HighlightLayer ), new FrameworkPropertyMetadata( null, OnRangesPropertyChanged ) );

        /// <summary>
        /// Handles change of the HighlightBrush dependency property.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="eventArgs">Event arguments.</param>
        private static void OnRangesPropertyChanged( DependencyObject sender, DependencyPropertyChangedEventArgs eventArgs )
        {
            HighlightLayer control = sender as HighlightLayer;
            if( control != null )
                control.OnRangesChanged( eventArgs.NewValue as IEnumerable<Tuple<int, int, int, int>> );
        }

        /// <summary>
        /// Handles change of the text property.
        /// </summary>
        /// <param name="text">New text.</param>
        protected virtual void OnRangesChanged( IEnumerable<Tuple<int, int, int, int>> ranges )
        {
        }

        /// <summary>
        /// HighlightBrush property.
        /// 
        /// HighlightBrush, which should be used for highlighting elements in the layer.
        /// </summary>
        public IEnumerable<Tuple<int, int, int, int>> Ranges
        {
            get { return GetValue( RangesProperty ) as IEnumerable<Tuple<int, int, int, int>>; }
            set { SetValue( RangesProperty, value ); }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new HighlightLayer();
        }
    }

    /// <summary>
    /// ColorSpanLayer class.
    /// 
    /// This implements a coloring layer. It allows to specify color spans for each line of text. 
    /// </summary>
    public class ColorSpanLayer
    {
        /// <summary>
        /// Color span class.
        /// </summary>
        public class ColorSpan
        {
            public int Start;
            public int End;
            public Brush Color;
        }

        /// <summary>
        /// Default brush of the layer. This is used whenever no brushes have been provided for a span.
        /// </summary>
        Brush mDefaultBrush;
        /// <summary>
        /// Color span dictionary storing a list of color spans.
        /// </summary>
        private Dictionary<int, List<ColorSpan>> mColorSpans = new Dictionary<int, List<ColorSpan>>();

        /// <summary>
        /// Returns or sets the default layer brush.
        /// </summary>
        public Brush DefaultBrush
        {
            get { return mDefaultBrush; }
            set 
            {
                if( mDefaultBrush != value )
                {
                    mDefaultBrush = value;
                    OnChanged();
                }
            }
        }

        /// <summary>
        /// Returns the spans for the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which all colors spans should be returned.</param>
        /// <returns>Enumeration of colors spans for the specified line, or null, if no color spans exist for current line.</returns>
        public IEnumerable<ColorSpan> GetColorSpansForLine( int lineIndex )
        {
            List<ColorSpan> spansInLine = null;
            if( mColorSpans.TryGetValue( lineIndex, out spansInLine ) )
                return spansInLine;
            return null;
        }

        /// <summary>
        /// Returns the number of colors spans in the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which the number of color spans should be returned.</param>
        /// <returns>Number of colors spans, contained in the specified line.</returns>
        public int GetNumberOfColorSpansForLine( int lineIndex )
        {
            List<ColorSpan> spansInLine = null;
            if( mColorSpans.TryGetValue( lineIndex, out spansInLine ) )
                return spansInLine.Count;
            return 0;
        }

        /// <summary>
        /// Color span comparer class.
        /// 
        /// This class is used for comparison of color spans. It compares their
        /// start position. It is used for binary search.
        /// </summary>
        private class ColorSpanComparer : IComparer< ColorSpan >
        {
            public int Compare( ColorSpan x, ColorSpan y )
            {
                // Compare start offset only.
                return x.Start - y.Start;
            }
        }

        /// <summary>
        /// Adds a color span for the specified line.
        /// 
        /// If the color span overlaps other spans they are removed.
        /// </summary>
        /// <param name="lineIndex">Index of the line for which a color span should be added.</param>
        /// <param name="startOffset">Start offset of the color span within the line.</param>
        /// <param name="endOffset">End offset of the color span within the line.</param>
        /// <param name="color">Color of the color span. If set to null, the default layer color will be used.</param>
        public void Add( int lineIndex, int startOffset, int endOffset, Brush color = null )
        {
            List<ColorSpan> spansInLine = null;
            if( !mColorSpans.TryGetValue( lineIndex, out spansInLine ) )
            {
                spansInLine = new List<ColorSpan>();
                mColorSpans.Add( lineIndex, spansInLine );
            }

            ColorSpan colorSpan = new ColorSpan { Start = startOffset, End = endOffset, Color = color };

            // Find insertion position
            int index = spansInLine.BinarySearch( colorSpan, new ColorSpanComparer() );
            if( index < 0 )
                index = -index;
            if( index >= spansInLine.Count )
            {
                // Append
                spansInLine.Add( colorSpan );
            }
            else
            {
                // Insert
                spansInLine.Insert( index, colorSpan );
                // Remove all color spans within the new color span.
                int removeCount = 0;
                for( int i = index + 1 ; i < spansInLine.Count ; ++i )
                {
                    int otherColorSpanEnd = spansInLine[i].End;
                    if( otherColorSpanEnd <= colorSpan.End )
                        ++removeCount;
                }
                spansInLine.RemoveRange( index + 1, removeCount );
                // Check, whether the previous and next span must be truncated.
                int previousIndex = index - 1;
                int nextIndex = index + 1;
                if( nextIndex < spansInLine.Count )
                {
                    var nextSpan = spansInLine[nextIndex];
                    if( nextSpan.Start < colorSpan.End )
                    {
                        // Truncate.
                        nextSpan.Start = colorSpan.Start;
                        // Remove, if empty.
                        if( nextSpan.End <= nextSpan.Start )
                            spansInLine.RemoveAt( nextIndex );
                    }
                }
                if( previousIndex >= 0 )
                {
                    var previousSpan = spansInLine[previousIndex];
                    if( previousSpan.End > colorSpan.Start )
                    {
                        // Truncate.
                        previousSpan.End = colorSpan.Start;
                        // Remove, if empty.
                        if( previousSpan.End <= previousSpan.Start )
                            spansInLine.RemoveAt( previousIndex );
                    }
                }
            }
            OnChanged( lineIndex );
        }

        /// <summary>
        /// Removes a color span from the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of the line, from which a color span should be removed.</param>
        /// <param name="spanIndex">Index of the span to be removed.</param>
        public void Remove( int lineIndex, int spanIndex )
        {
            List<ColorSpan> spansInLine = null;
            if( !mColorSpans.TryGetValue( lineIndex, out spansInLine ) )
            {
                spansInLine.RemoveAt( spanIndex );
                OnChanged( lineIndex );
            }
        }

        /// <summary>
        /// Removes all color spans within the specified range. If color spans overlap the range, they are 
        /// truncated.
        /// </summary>
        /// <param name="lineIndex">Index of the line from which color spans should be removed.</param>
        /// <param name="startOffset">Start offset of the range within the line, inside which color spans should be removed.</param>
        /// <param name="endOffset">End offset of the range within the line, inside which colors spans should be removed.</param>
        public void Remove( int lineIndex, int startOffset, int endOffset )
        {
            List<ColorSpan> spansInLine = null;
            if( !mColorSpans.TryGetValue( lineIndex, out spansInLine ) )
                return;

            ColorSpan colorSpan = new ColorSpan { Start = startOffset, End = endOffset, Color = null };

            // Find insertion position
            int index = spansInLine.BinarySearch( colorSpan, new ColorSpanComparer() );
            if( index < 0 )
                index = -index - 1;

            bool changed = false;

            // Remove all color spans within the new color span.
            int removeCount = 0;
            for( int i = index ; i < spansInLine.Count ; ++i )
            {
                int otherColorSpanEnd = spansInLine[i].End;
                if( otherColorSpanEnd <= colorSpan.End )
                    ++removeCount;
            }

            changed = removeCount > 0;

            spansInLine.RemoveRange( index + 1, removeCount );
            // Check, whether the previous and next span must be truncated.
            int previousIndex = index;
            int nextIndex = index + 1;
            if( nextIndex < spansInLine.Count )
            {
                var nextSpan = spansInLine[nextIndex];
                if( nextSpan.Start < colorSpan.End )
                {
                    // Truncate.
                    nextSpan.Start = colorSpan.Start;
                    // Remove, if empty.
                    if( nextSpan.End <= nextSpan.Start )
                    {
                        spansInLine.RemoveAt( nextIndex );
                        changed = true;
                    }
                }
            }
            if( previousIndex >= 0 )
            {
                var previousSpan = spansInLine[previousIndex];
                if( previousSpan.End > colorSpan.Start )
                {
                    // Truncate.
                    previousSpan.End = colorSpan.Start;
                    // Remove, if empty.
                    if( previousSpan.End <= previousSpan.Start )
                    {
                        spansInLine.RemoveAt( previousIndex );
                        changed = true;
                    }
                }
            }

            if( changed )
                OnChanged( lineIndex );
        }

        /// <summary>
        /// Clears all color spans for the specified line.
        /// </summary>
        /// <param name="lineIndex">Line index for which all color spans should be removed.</param>
        public void Clear( int lineIndex )
        {
            List<ColorSpan> spansInLine = null;
            if( mColorSpans.TryGetValue( lineIndex, out spansInLine ) )
            {
                spansInLine.Clear();
                OnChanged( lineIndex );
            }
        }

        /// <summary>
        /// Clears the whole color span layer.
        /// </summary>
        public void Clear()
        {
            mColorSpans.Clear();
            OnChanged();
        }

        /// <summary>
        /// Issues the Changed event.
        /// </summary>
        /// <param name="lineIndex">Index of the line, which changed.</param>
        protected virtual void OnChanged( int lineIndex )
        {
            if( Changed != null )
                Changed( this, lineIndex );
        }

        /// <summary>
        /// Issues the Changed event.
        /// </summary>
        protected virtual void OnChanged()
        {
            OnChanged( -1 );
        }

        /// <summary>
        /// Change event.
        /// 
        /// This event is raised every time the color span layer has changed.
        /// </summary>
        public event Action<ColorSpanLayer, int> Changed;
    }

    /// <summary>
    /// Line number viewer visual.
    /// 
    /// This visual is used for displaying line numbers by the TextViewerControl.
    /// </summary>
    class LineNumberViewer : FrameworkElement
    {
        /// <summary>
        /// LineMouseOver routed event.
        /// </summary>
        public static RoutedEvent LineMouseOverEvent = EventManager.RegisterRoutedEvent( "LineMouseOver", RoutingStrategy.Bubble, typeof( LineHitEventHandler ), typeof( LineNumberViewer ) );

        /// <summary>
        /// LineMouseOver event.
        /// 
        /// The LineMouseOver event is raised every time the mouse cursor hovers over a piece of Line.
        /// </summary>
        public event LineHitEventHandler LineMouseOver
        {
            add { AddHandler( LineMouseOverEvent, value ); }
            remove { RemoveHandler( LineMouseOverEvent, value ); }
        }

        /// <summary>
        /// LineMouseDown routed event.
        /// </summary>
        public static RoutedEvent LineMouseDownEvent = EventManager.RegisterRoutedEvent( "LineMouseDown", RoutingStrategy.Bubble, typeof( LineHitEventHandler ), typeof( LineNumberViewer ) );

        /// <summary>
        /// LineMouseDown event.
        /// 
        /// The LineMouseDown event is raised every time a mouse button is pressed over a piece of Line.
        /// </summary>
        public event LineHitEventHandler LineMouseDown
        {
            add { AddHandler( LineMouseDownEvent, value ); }
            remove { RemoveHandler( LineMouseDownEvent, value ); }
        }

        /// <summary>
        /// LineMouseUp routed event.
        /// </summary>
        public static RoutedEvent LineMouseUpEvent = EventManager.RegisterRoutedEvent( "LineMouseUp", RoutingStrategy.Bubble, typeof( LineHitEventHandler ), typeof( LineNumberViewer ) );

        /// <summary>
        /// LineMouseUp event.
        /// 
        /// The LineMouseUp event is raised every time a mouse button is released over a piece of Line.
        /// </summary>
        public event LineHitEventHandler LineMouseUp
        {
            add { AddHandler( LineMouseDownEvent, value ); }
            remove { RemoveHandler( LineMouseDownEvent, value ); }
        }

        /// <summary>
        /// Parent TextViewerControl.
        /// </summary>
        TextViewerControl mParent;

        /// <summary>
        /// Font to be used for text rendering.
        /// </summary>
        System.Windows.Media.Typeface mFont;
        /// <summary>
        /// Size of the font to be used for text rendering.
        /// </summary>
        double mFontSize;
        /// <summary>
        /// Line height;
        /// </summary>
        double mLineHeight = 1.0;
        /// <summary>
        /// Text foreground brush, to be used for text rendering.
        /// </summary>
        Brush mForeground;

        /// <summary>
        /// Array of FormattedText objects for each line.
        /// </summary>
        FormattedText[] mFormattedTextForLine;
        /// <summary>
        /// Index of the top most line, which should be displayed. This is used for vertical scrolling.
        /// </summary>
        int mTopLine;
        /// <summary>
        /// Total number of lines.
        /// </summary>
        int mLineCount;

        /// <summary>
        /// Number of lines currently visible in viewport.
        /// </summary>
        int mViewportLineCount;
        /// <summary>
        /// Y position of each line currently visible in the viewport.
        /// </summary>
        double[] mViewportLineVerticalPosition;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="parent">Parent TextViewerControl.</param>
        public LineNumberViewer( TextViewerControl parent )
        {
            Cursor = Cursors.Hand;
            ClipToBounds = true;
            mParent = parent;
            AttachToParent();
            UpdateVisualAttributes();
            UpdateFont();
        }

        /// <summary>
        /// Attaches to the parent text viewer control.
        /// 
        /// This method attaches to some of the dependency properties of the TextViewerControl, which affect text rendering.
        /// </summary>
        private void AttachToParent()
        {
            if( mParent != null )
            {
                DependencyPropertyDescriptor.FromProperty( UserControl.FontFamilyProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontSizeProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontStretchProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontWeightProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.FontStyleProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentFontPropertyChanged );
                DependencyPropertyDescriptor.FromProperty( UserControl.ForegroundProperty, typeof( UserControl ) ).AddValueChanged( mParent, OnParentVisualPropertyChanged );
            }
        }

        /// <summary>
        /// Updates the font to be used for text rendering. By fetching it from the parent TextViewerControl.
        /// </summary>
        private void UpdateFont()
        {
            if( mParent == null )
                return;
            mFont = CreateTypeFace( mParent );
            mFontSize = mParent.FontSize * 96.0 / 72.0;
            mLineHeight = new FormattedText( " gy", System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, mFont, mFontSize, SystemColors.ControlTextBrush ).Height;
            InvalidateLines();
        }

        /// <summary>
        /// Updates the visual attributes, which affect text rendering, by fetching them from the parent TextViewerControl.
        /// </summary>
        private void UpdateVisualAttributes()
        {
            if( mParent == null )
                return;
            mForeground = mParent.Foreground;
        }

        /// <summary>
        /// Invalidates all FormattedText objects, which have been created so far for each individual line.
        /// </summary>
        private void InvalidateLines()
        {
            mFormattedTextForLine = new FormattedText[mLineCount];
        }

        /// <summary>
        /// Handles the change of any font related property of the parent TextViewerControl.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnParentFontPropertyChanged( object sender, EventArgs e )
        {
            UpdateFont();
            InvalidateVisual();
        }

        /// <summary>
        /// Handles the change of any visual related property of the parent TextViewerControl.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        private void OnParentVisualPropertyChanged( object sender, EventArgs e )
        {
            UpdateVisualAttributes();
            InvalidateVisual();
        }

        /// <summary>
        /// Creates a typeface object based on the font settings of the parent TextViewerControl.
        /// </summary>
        /// <param name="parent">Parent text viewer control.</param>
        /// <returns>Typeface object representing the font of the parent TextViewerControl.</returns>
        private static Typeface CreateTypeFace( TextViewerControl mParent )
        {
            return new Typeface( mParent.FontFamily, mParent.FontStyle, mParent.FontWeight, mParent.FontStretch );
        }

        /// <summary>
        /// Returns the number of lines in the text, which is displayed.
        /// </summary>
        public int LineCount
        {
            get
            {
                return mLineCount;
            }
            set
            {
                if( mLineCount != value )
                {
                    mLineCount = value;
                    InvalidateLines();
                    InvalidateMeasure();
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Returns the maximum horizontal size of the displayed text.
        /// </summary>
        public double HorizontalTextSize
        {
            get
            {
                var longestLineText = GetFormattedTextForLine( mLineCount - 1 );
                return longestLineText != null ? longestLineText.Width : 0.0;
            }
        }

        /// <summary>
        /// Returns the vertical size of the current viewport given in text lines.
        /// </summary>
        public double VerticalViewportSize
        {
            get { return Math.Floor( ActualHeight / mLineHeight ); }
        }

        /// <summary>
        /// Returns the horizontal size of the viewport given in logical display units.
        /// </summary>
        public double HorizontalViewportSize
        {
            get { return ActualWidth; }
        }

        /// <summary>
        /// Returns or changes the index of the top most line, which should be displayed. This is used for vertical scrolling.
        /// </summary>
        public int TopLine
        {
            get { return mTopLine; }
            set
            {
                // Clamp value
                value = Math.Min( Math.Max( 0, value ), LineCount );
                if( mTopLine != value )
                {
                    mTopLine = value;
                    InvalidateVisual();
                }
            }
        }

        /// <summary>
        /// Returns the text of the specified line as a string.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which the text should be returned.</param>
        /// <returns>Text of the specified line, as a string.</returns>
        protected string GetLine( int lineIndex )
        {
            return ( lineIndex + 1 ).ToString();
        }

        /// <summary>
        /// Returns the formatted text object for the specified line.
        /// </summary>
        /// <param name="lineIndex">Index of the line, for which a formatted text should be returned.</param>
        /// <returns>FormattedText object of the specified line.</returns>
        protected FormattedText GetFormattedTextForLine( int lineIndex )
        {
            if( mFormattedTextForLine == null )
                return null;

            if( mFormattedTextForLine[lineIndex] == null )
            {
                mFormattedTextForLine[lineIndex] = new FormattedText( GetLine( lineIndex ), System.Globalization.CultureInfo.CurrentUICulture, System.Windows.FlowDirection.LeftToRight, mFont, mFontSize, mForeground );
                mFormattedTextForLine[lineIndex].TextAlignment = TextAlignment.Right;
            }
            return mFormattedTextForLine[lineIndex];
        }

        /// <summary>
        /// Measures the control.
        /// </summary>
        /// <param name="availableSize">Available size.</param>
        /// <returns>Desired size.</returns>
        protected override Size MeasureOverride( Size availableSize )
        {
            return new Size( HorizontalTextSize + 5, 0 );
        }

        /// <summary>
        /// Handles the mouse down event.
        /// </summary>
        /// <param name="e>Event args.</param>
        protected override void OnMouseDown( MouseButtonEventArgs e )
        {
            base.OnMouseDown( e );

            // Get text hit by the mouse cursor.
            var mousePosition = e.GetPosition( this );
            var lineIndex = LineHitTest( mousePosition );
            if( lineIndex >= 0 )
                RaiseEvent( new LineHitEventArgs( LineMouseDownEvent, this, lineIndex, e ) );
        }

        /// <summary>
        /// Handles the mouse move event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected override void OnMouseMove( MouseEventArgs e )
        {
            base.OnMouseMove( e );

            // Get text hit by the mouse cursor.
            var mousePosition = e.GetPosition( this );
            var lineIndex = LineHitTest( mousePosition );
            if( lineIndex >= 0 )
                RaiseEvent( new LineHitEventArgs( LineMouseOverEvent, this, lineIndex, e ) );
        }

        /// <summary>
        /// Handles the mouse up event.
        /// </summary>
        /// <param name="e">Event args.</param>
        protected override void OnMouseUp( MouseButtonEventArgs e )
        {
            base.OnMouseUp( e );

            // Get text hit by the mouse cursor.
            var mousePosition = e.GetPosition( this );
            var lineIndex = LineHitTest( mousePosition );
            if( lineIndex >= 0 )
                RaiseEvent( new LineHitEventArgs( LineMouseUpEvent, this, lineIndex, e ) );
        }

        /// <summary>
        /// Handles rendering.
        /// </summary>
        /// <param name="drawingContext">Drawing context to be used for rendering.</param>
        protected override void OnRender( DrawingContext drawingContext )
        {
            // Prefetch values.
            double actualHeight = ActualHeight;
            double actualWidth = ActualWidth;
            int lineCount = LineCount;

            // Prepare viewport related structures
            int numberOfVisibleLines = (int) Math.Ceiling( actualHeight / mLineHeight ) + 1;
            if( mViewportLineVerticalPosition == null || numberOfVisibleLines > mViewportLineVerticalPosition.Length )
                mViewportLineVerticalPosition = new double[numberOfVisibleLines];

            // Create brushes and pens used for background rendering.
            var separatorLinePen = new Pen( new SolidColorBrush( System.Windows.Media.Colors.Black ), 1.0 );
            separatorLinePen.Brush.Opacity = 0.5;

            var alternatingRowBackgroundBrushOdd = new SolidColorBrush( System.Windows.Media.Colors.Black );
            alternatingRowBackgroundBrushOdd.Opacity = 0.15;

            var alternatingRowBackgroundBrushEven = new SolidColorBrush( System.Windows.Media.Colors.Black );
            alternatingRowBackgroundBrushEven.Opacity = 0.08;

            // Draw vertical separator line
            drawingContext.DrawLine( separatorLinePen, new Point( actualWidth - 1.0, 0 ), new Point( ActualWidth - 1.0, ActualHeight ) );

            Rect backgroundRect = new Rect( 0, 0, actualWidth - 2.0, 0 );
            Point origin = new Point( actualWidth - 3, 0.0 );
            // Check, if anything has to be rendered.
            mViewportLineCount = 0;
            if( mTopLine < lineCount )
            {            
                // Draw line numbers
                for( int lineIndex = mTopLine ; lineIndex < lineCount ; ++lineIndex )
                {
                    mViewportLineVerticalPosition[mViewportLineCount] = origin.Y;

                    // Skip, if outside viewport.
                    if( origin.Y > actualHeight )
                        break;

                    var lineText = GetFormattedTextForLine( lineIndex );
                    
                    // Draw alternating background
                    backgroundRect.Y = origin.Y;
                    backgroundRect.Height = lineText.Height;
                    drawingContext.DrawRectangle( ( ( lineIndex & 1 ) == 0 ) ? alternatingRowBackgroundBrushEven : alternatingRowBackgroundBrushOdd, null, backgroundRect );
                    // Draw line number
                    drawingContext.DrawText( lineText, origin );
                    // Update rendering position
                    origin.Y += lineText.Height;
                    ++mViewportLineCount;
                }
            }
        }

        /// <summary>
        /// Performs a line hit test at the specified position.
        /// </summary>
        /// <param name="position">Position at which the line should be hit.</param>
        /// <returns>Index of the line, which was hit, or -1 if no line was hit.</returns>
        public int LineHitTest( Point position )
        {
            if( mViewportLineVerticalPosition != null )
            {
                var lineIndex = Array.BinarySearch( mViewportLineVerticalPosition, 0, mViewportLineCount, position.Y );
                if( lineIndex < 0 )
                    lineIndex = -lineIndex - 2;
                return lineIndex + mTopLine;
            }
            return -1;
        }
    }

    /// <summary>
    /// TextHitInfo class.
    /// </summary>
    public class TextHitInfo
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textPosition">Text position of the text hit.</param>
        /// <param name="textOffset">Text offset of the text hit.</param>
        internal TextHitInfo( TextPosition textPosition, int textOffset )
        {
            TextPosition = textPosition;
            TextOffset = textOffset;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="textPosition">Text position of the text hit.</param>
        /// <param name="textOffset">Text offset of the text hit.</param>
        /// <param name="highlighterToken">Highlighter token, which has been hit.</param>
        /// <param name="highlighterTokenText">Text of the highlighter token, which has been hit.</param>
        internal TextHitInfo( TextPosition textPosition, int textOffset, IHighlighterToken highlighterToken, string highlighterTokenText )
        {
            TextPosition = textPosition;
            TextOffset = textOffset;
            HighlighterToken = highlighterToken;
            HighlighterTokenText = highlighterTokenText;
        }

        public TextPosition TextPosition { get; private set; }
        public int TextOffset { get; private set; }
        public IHighlighterToken HighlighterToken { get; private set; }
        public string HighlighterTokenText { get; private set; }
    }

    /// <summary>
    /// TextHitEvent argument class.
    /// </summary>
    public class TextHitEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Text hit info.
        /// </summary>
        public TextHitInfo TextHitInfo { get; private set; }
        /// <summary>
        /// Arguments of the underlying mouse event.
        /// </summary>
        public MouseEventArgs MouseEventArgs { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="routedEvent">Routed event.</param>
        /// <param name="source">Event source.</param>
        /// <param name="textHitInfo">TextHitInfo structure.</param>
        /// <param name="mouseEventArgs">Mouse event arguments.</param>
        internal TextHitEventArgs( RoutedEvent routedEvent, object source, TextHitInfo textHitInfo, MouseEventArgs mouseEventArgs )
            : base( routedEvent, source )
        {
            TextHitInfo = textHitInfo;
            MouseEventArgs = mouseEventArgs;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="routedEvent">Routed event.</param>
        /// <param name="source">Event source.</param>
        /// <param name="position">Text position, which has been hit.</param>
        /// <param name="offset">Text offset corresponding to the text position, which has been hit.</param>
        /// <param name="mouseEventArgs">Mouse event arguments.</param>
        internal TextHitEventArgs( RoutedEvent routedEvent, object source, TextPosition position, int offset, IHighlighterToken highlighterToken, string highlighterTokenText, MouseEventArgs mouseEventArgs )
            : base( routedEvent, source )
        {
            TextHitInfo = new TextHitInfo( position, offset, highlighterToken, highlighterTokenText );
            MouseEventArgs = mouseEventArgs;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="routedEvent">Routed event.</param>
        /// <param name="position">Text position, which has been hit.</param>
        /// <param name="offset">Text offset corresponding to the text position, which has been hit.</param>
        /// <param name="mouseEventArgs">Mouse event arguments.</param>
        internal TextHitEventArgs( RoutedEvent routedEvent, TextPosition position, int offset, IHighlighterToken highlighterToken, string highlighterTokenText, MouseEventArgs mouseEventArgs )
            : this( routedEvent, null, position, offset, highlighterToken, highlighterTokenText, mouseEventArgs )
        {
        }
    }

    /// <summary>
    /// Text hit event handler delegate.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    public delegate void TextHitEventHandler( object sender, TextHitEventArgs e );

    /// <summary>
    /// LineHitEvent argument class.
    /// </summary>
    public class LineHitEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// LineIndex.
        /// </summary>
        public int LineIndex { get; private set; }
        /// <summary>
        /// Arguments of the underlying mouse event.
        /// </summary>
        public MouseEventArgs MouseEventArgs { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="routedEvent">Routed event.</param>
        /// <param name="source">Event source.</param>
        /// <param name="lineIndex">Index of the line, which has been hit.</param>
        /// <param name="mouseEventArgs">Mouse event arguments.</param>
        internal LineHitEventArgs( RoutedEvent routedEvent, object source, int lineIndex, MouseEventArgs mouseEventArgs )
            : base( routedEvent, source )
        {
            LineIndex = lineIndex;
            MouseEventArgs = mouseEventArgs;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="routedEvent">Routed event.</param>
        /// <param name="lineIndex">Index of the line, which has been hit.</param>
        /// <param name="mouseEventArgs">Mouse event arguments.</param>
        internal LineHitEventArgs( RoutedEvent routedEvent, int lineIndex, MouseEventArgs mouseEventArgs )
            : base( routedEvent )
        {
            LineIndex = lineIndex;
            MouseEventArgs = mouseEventArgs;
        }

    }


    /// <summary>
    /// Line hit event handler delegate.
    /// </summary>
    /// <param name="sender">Event sender.</param>
    /// <param name="e">Event arguments.</param>
    public delegate void LineHitEventHandler( object sender, LineHitEventArgs e );

}
