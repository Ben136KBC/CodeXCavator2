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
using CodeXCavator.Engine.Interfaces;
using System.Windows.Media;
using System.Windows.Input;

namespace CodeXCavator.UI
{
    public class OccurrenceControl : Control
    {
        /// <summary>
        /// SelectedOccurrenceChanged routed event.
        /// </summary>
        public static readonly RoutedEvent SelectedOccurrenceChangedEvent = EventManager.RegisterRoutedEvent("SelectedOccurrenceChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(OccurrenceControl));

        /// <summary>
        /// SelectedOccurrenceChanged event.
        /// </summary>
        public event RoutedEventHandler SelectedOccurrenceChanged
        {
            add { AddHandler(SelectedOccurrenceChangedEvent, value); } 
            remove { RemoveHandler(SelectedOccurrenceChangedEvent, value); }
        }

        // Raises the SelectedOccurrenceChanged event.
        protected void RaiseSelectedOccurrenceChangedEvent()
        {
            RoutedEventArgs newEventArgs = new RoutedEventArgs(OccurrenceControl.SelectedOccurrenceChangedEvent);
            RaiseEvent(newEventArgs);
        }

        private const double OCCURRENCE_INDICATOR_THICKNESS = 3.0;
        private const double OCCURRENDE_INDICATOR_PADDING = 3.0;

        /// <summary>
        /// SelectedOccurrence dependency property
        /// </summary>
        public static readonly DependencyProperty SelectedOccurrenceProperty = DependencyProperty.Register( "SelectedOccurrence", typeof( ViewModel.OccurrenceViewModel ), typeof( OccurrenceControl ), new PropertyMetadata( null, OnSelectedOccurrenceChanged ) );

        /// <summary>
        /// Handles the change of the SelectedOccurrence dependency property.
        /// </summary>
        /// <param name="d">Dependency object containing the property.</param>
        /// <param name="e">Property change event arguments.</param>
        private static void OnSelectedOccurrenceChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( (OccurrenceControl) d ).OnSelectedOccurrenceChanged( e.OldValue as ViewModel.OccurrenceViewModel, e.NewValue as ViewModel.OccurrenceViewModel );
        }

        /// <summary>
        /// Handles the change of the Occurrences property.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected virtual void OnSelectedOccurrenceChanged( ViewModel.OccurrenceViewModel oldValue, ViewModel.OccurrenceViewModel newValue )
        {
            RaiseSelectedOccurrenceChangedEvent();
        }

        /// <summary>
        /// Sets or returns the list of indexes, on which search can be performed using the search control.
        /// </summary>
        public ViewModel.OccurrenceViewModel SelectedOccurrence
        {
            get
            {
                return GetValue( SelectedOccurrenceProperty ) as ViewModel.OccurrenceViewModel;
            }
            set
            {
                SetValue( SelectedOccurrenceProperty, value );
            }
        }

        /// <summary>
        /// Occurrences dependency property
        /// </summary>
        public static readonly DependencyProperty OccurrencesProperty = DependencyProperty.Register( "Occurrences", typeof( IEnumerable<IOccurrence> ), typeof( OccurrenceControl ), new FrameworkPropertyMetadata( null, FrameworkPropertyMetadataOptions.AffectsRender, OnOccurrencesChanged ) );

        /// <summary>
        /// Handles the change of the Occurrences dependency property.
        /// </summary>
        /// <param name="d">Dependency object containing the property.</param>
        /// <param name="e">Property change event arguments.</param>
        private static void OnOccurrencesChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( (OccurrenceControl) d ).OnOccurrencesChanged( e.OldValue as IEnumerable<IOccurrence>, e.NewValue as IEnumerable<IOccurrence> );
        }

        /// <summary>
        /// Handles the change of the Occurrences property.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected virtual void OnOccurrencesChanged( IEnumerable<IOccurrence> oldValue, IEnumerable<IOccurrence> newValue )
        {
            if( newValue != null )
            {
                mOccurrences = newValue.ToArray();
                mOccurrenceYPositions = new double[mOccurrences.Length];
            }
            else
            {
                mOccurrences = null;
                mOccurrenceYPositions = null;
            }
        }

        /// <summary>
        /// Sets or returns the list of indexes, on which search can be performed using the search control.
        /// </summary>
        public IEnumerable<IOccurrence> Occurrences
        {
            get
            {
                return GetValue( OccurrencesProperty ) as IEnumerable<IOccurrence>;
            }
            set
            {
                SetValue( OccurrencesProperty, value );
            }
        }

        /// <summary>
        /// LineCount dependency property
        /// </summary>
        public static readonly DependencyProperty LineCountProperty = DependencyProperty.Register( "LineCount", typeof( int ), typeof( OccurrenceControl ), new FrameworkPropertyMetadata( 0, FrameworkPropertyMetadataOptions.AffectsRender, OnLineCountChanged ) );

        /// <summary>
        /// Handles the change of the LineCount dependency property.
        /// </summary>
        /// <param name="d">Dependency object containing the property.</param>
        /// <param name="e">Property change event arguments.</param>
        private static void OnLineCountChanged( DependencyObject d, DependencyPropertyChangedEventArgs e )
        {
            ( (OccurrenceControl) d ).OnLineCountChanged( (int) e.OldValue, (int) e.NewValue );
        }

        /// <summary>
        /// Handles the change of the LineCount property.
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        protected virtual void OnLineCountChanged( int oldValue, int newValue )
        {
        }

        /// <summary>
        /// Sets or returns the list of indexes, on which search can be performed using the search control.
        /// </summary>
        public int LineCount
        {
            get
            {
                return (int) GetValue( LineCountProperty );
            }
            set
            {
                SetValue( LineCountProperty, value );
            }
        }

        protected double[] mOccurrenceYPositions;
        protected IOccurrence[] mOccurrences;

        /// <summary>
        /// Renders the control.
        /// </summary>
        /// <param name="drawingContext">Drawing context to be used for rendering.</param>
        protected override void OnRender( DrawingContext drawingContext )
        {
            base.OnRender( drawingContext );
            var occurrences = Occurrences;
            int lineCount = LineCount;
            double controlWidth = this.RenderSize.Width;
            double controlHeight = this.RenderSize.Height;
            var foreground = Foreground;
            drawingContext.DrawRectangle( Background, null, new Rect( this.RenderSize ) );
            double lastIndicatorYPosition = -OCCURRENCE_INDICATOR_THICKNESS;
            if( occurrences != null )
            {
                int currentOccurrenceIndex = 0;
                foreach( var occurrence in occurrences )
                {
                    double currentIndicatorYPosition = Math.Floor( ( ( controlHeight - OCCURRENCE_INDICATOR_THICKNESS ) * (double) ( occurrence.Line ) ) / (double) ( LineCount - 1 ) );
                    if( currentIndicatorYPosition - lastIndicatorYPosition >= OCCURRENCE_INDICATOR_THICKNESS )
                    {
                        mOccurrenceYPositions[currentOccurrenceIndex] = currentIndicatorYPosition;
                        drawingContext.DrawRectangle( foreground, null, new Rect( OCCURRENDE_INDICATOR_PADDING, currentIndicatorYPosition, controlWidth - OCCURRENDE_INDICATOR_PADDING * 2.0, OCCURRENCE_INDICATOR_THICKNESS ) );
                        lastIndicatorYPosition = currentIndicatorYPosition;
                    }
                    else
                    {
                        mOccurrenceYPositions[currentOccurrenceIndex] = lastIndicatorYPosition;
                    }
                    ++currentOccurrenceIndex;
                }
            }
        }

        protected int mLastToolTipOccurrenceIndex = -1;

        private int GetIndexOfClosestOccurrence( double mouseY )
        {
            var closestOccurrenceIndex = Array.BinarySearch<double>( mOccurrenceYPositions, mouseY );
            if( closestOccurrenceIndex < 0 )
                closestOccurrenceIndex = -closestOccurrenceIndex - 1;
            if( closestOccurrenceIndex < 0 )
                return -1;
            if( closestOccurrenceIndex > mOccurrenceYPositions.Length - 1 )
                closestOccurrenceIndex = mOccurrenceYPositions.Length - 1;
            while( closestOccurrenceIndex >= 0 && ( mOccurrenceYPositions[closestOccurrenceIndex] + OCCURRENCE_INDICATOR_THICKNESS ) > mouseY )
                --closestOccurrenceIndex;
            if( closestOccurrenceIndex >= 0 )
            {
                double closestOccurrencePosition = mOccurrenceYPositions[closestOccurrenceIndex];
                var distanceToClosestOccurrence = mouseY - closestOccurrencePosition;
                if( distanceToClosestOccurrence >= 0.0 && distanceToClosestOccurrence < OCCURRENCE_INDICATOR_THICKNESS )
                    return closestOccurrenceIndex;
            }
            while( closestOccurrenceIndex < mOccurrenceYPositions.Length && ( closestOccurrenceIndex < 0 || mOccurrenceYPositions[closestOccurrenceIndex] + OCCURRENCE_INDICATOR_THICKNESS <= mouseY ) )
                closestOccurrenceIndex++;
            if( closestOccurrenceIndex < mOccurrences.Length )
            {
                double closestOccurrencePosition = mOccurrenceYPositions[closestOccurrenceIndex];
                var distanceToClosestOccurrence = mouseY - closestOccurrencePosition;
                if( distanceToClosestOccurrence >= 0.0 && distanceToClosestOccurrence < OCCURRENCE_INDICATOR_THICKNESS )
                    return closestOccurrenceIndex;
            }

            return -1;
        }

        protected override void OnMouseLeave( System.Windows.Input.MouseEventArgs e )
        {
            base.OnMouseLeave( e );
            var currentToolTip = ToolTip as ToolTip;
            if( currentToolTip != null )
            {
                currentToolTip.IsOpen = false;
                ToolTip = null;
            }
        }

        protected override void OnMouseMove( System.Windows.Input.MouseEventArgs e )
        {
            base.OnMouseMove( e );
            var currentToolTip = ToolTip as ToolTip;
            if( mOccurrenceYPositions != null )
            {
                double mouseY = Math.Floor( e.GetPosition( this ).Y );
                int closestOccurrenceIndex = GetIndexOfClosestOccurrence( mouseY );
                if( mLastToolTipOccurrenceIndex != closestOccurrenceIndex )
                {
                    mLastToolTipOccurrenceIndex = closestOccurrenceIndex;
                    if( currentToolTip != null )
                        currentToolTip.IsOpen = false;
                    if( closestOccurrenceIndex >= 0 )
                    {
                        var occurrenceToolTip = new ToolTip { Content = CreateToolTip( closestOccurrenceIndex, mOccurrenceYPositions[ closestOccurrenceIndex ] ) };
                        ToolTip = occurrenceToolTip;
                        occurrenceToolTip.IsOpen = true;
                        return;
                    }
                }
            }
            else
            {
                ToolTip = null;
                mLastToolTipOccurrenceIndex = -1;
                if( currentToolTip != null )
                    currentToolTip.IsOpen = false;
            }
        }

        protected override void OnMouseLeftButtonDown(System.Windows.Input.MouseButtonEventArgs e)
        {
 	        base.OnMouseLeftButtonDown(e);
            if( mOccurrenceYPositions != null )
            {
                double mouseY = Math.Floor( e.GetPosition( this ).Y );
                int closestOccurrenceIndex = GetIndexOfClosestOccurrence( mouseY );
                if( closestOccurrenceIndex >= 0 )
                {
                    SelectedOccurrence = new ViewModel.OccurrenceViewModel( mOccurrences[ closestOccurrenceIndex ], null );
                    return;
                }
            }
            SelectedOccurrence = null;
        }

        protected override void OnQueryCursor( System.Windows.Input.QueryCursorEventArgs e )
        {
            base.OnQueryCursor( e );
            if( mOccurrenceYPositions != null )
            {
                double mouseY = Math.Floor( e.GetPosition( this ).Y );
                int closestOccurrenceIndex = GetIndexOfClosestOccurrence( mouseY );
                if( closestOccurrenceIndex >= 0 )
                {
                    e.Cursor = Cursors.Hand;
                    e.Handled = true;
                }
            }

        }

        private string CreateToolTip( int closestOccurrenceIndex, double mouseY )
        {
            StringBuilder toolTipBuilder = new StringBuilder();
            bool firstOccurrence = true;
            for( int occurrenceIndex = closestOccurrenceIndex ; occurrenceIndex < mOccurrenceYPositions.Length && ( ( mOccurrenceYPositions[occurrenceIndex] - mouseY ) < OCCURRENCE_INDICATOR_THICKNESS ) ; ++occurrenceIndex )
            {
                if( !firstOccurrence )
                    toolTipBuilder.Append( "\n" );
                firstOccurrence = false;
                IOccurrence occurrence = mOccurrences[occurrenceIndex];
                toolTipBuilder.Append( occurrence.Match );
                toolTipBuilder.Append( ":" );
                toolTipBuilder.Append( "(" );
                toolTipBuilder.Append( occurrence.Line + 1 );
                toolTipBuilder.Append( "," );
                toolTipBuilder.Append( occurrence.Column + 1 );
                toolTipBuilder.Append( ")" );
            }
            return toolTipBuilder.ToString();
        }

    }
}
