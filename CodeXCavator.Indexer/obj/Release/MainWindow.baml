   M S B A M L   `   `   `  ���� Q  MCodeXCavator.Indexer, Version=0.10.0.11, Culture=neutral, PublicKeyToken=null;"clr-namespace:CodeXCavator.IndexerCodeXCavator.Indexer  %    CodeXCavator.Indexer.MainWindow   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     0l"clr-namespace:CodeXCavator.Indexer     -     � Title$  CodeXCavator - Indexer��$
��Auto�� � SizeToContent$ Height=�$	��800�� � 
ResizeMode$ NoResize=� � WindowStartupLocation$ CenterScreen=� � WindowStyle$ SingleBorderWindow=� � Topmost .   � Icon$8 2/CodeXCavator.Indexer;component/Images/doc_tag.png�� � 	Resources    	BoolToVis��%   &	        �� .��� $
��Auto��$
��Auto�� � RowDefinitions �� $
��Auto ��� $��* �.+��� ���	#30000000�� -   	 ��Name$	 imgInfo��$��0��$
��None=�$��Right=�$<��6/CodeXCavator.Indexer;component/Images/exclamation.png��$��5q�
 � ToolTip
 _� .����  �� Orientation$ Vertical=� ^� ShowDuration$
 5000��$��0,5,0,0q�.+��� $ 
Horizontal=�.+��� $5��//CodeXCavator.Indexer;component/Images/logo.png��$
��None=�$	��Top=�$��0,0,5,0q���  �� TextAlignment$ Justify=�$	��400��$
��Wrap=�$��Center=��� $��16(�$
��Bold"�
�������.�CodeXCavator�� �� 
�������.�
Version: �� 
�������*   $CodeXCavator.Indexer.AssemblyVersion   Version#�Z �� ����  �� EndPoint$ 1,0.5&� �� 
StartPoint$ 0,0.5&�.���  � Color$ 	#C01E90FF�� � Offset$	 0.5Q�� $ 	#001E90FF��$ 1Q��� $ Justify=�$	��400��$
��Wrap=�A?A tool for source code indexing and full text search using the �� .�Lucene library from the �� .�Apache Software Foundation.�� �� �� .�Icons:/- LED Icon Set v1.0 - http://led24.de/iconset/�� �� �� .�
License:�� �� .�.�Copyright 2014-�� $
��Bold"�   CurrentYear#�Z  Christoph Brzozowski�� �� Licensed under the �� .�Apache License, Version 2.0|z (the "License"); you may not use this file except in compliance with the License. You may obtain a copy of the License at�� �� �� .�,*http://www.apache.org/licenses/LICENSE-2.0�� �� ��Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the License for the specific language governing permissions and limitations under the License.�� $��1��$
��Auto�� �� AlternationCount$ 2������ *IndexBuilders+���� .R��� $ Vertical=�.+���  �� Padding$ 5q�$��2q� �� CornerRadius$ 1������ $ 0.5,0&�$ 0.5,1&�.��� $ 	#FFFFFFFF��$	 0.5Q�� $ 	#40000000��$ 1Q�._�]� ��. .+��� $
��Left=�$8��2/CodeXCavator.Indexer;component/Images/doc_tag.png��$
��None=� � UseLayoutRounding .  J� SnapsToDevicePixels . $��0,0,5,0q�� $��Right=� � ColumnDefinitions �� $
��Auto ��� $��* ��� $
��Auto ��� $
��Auto ��� $
��Auto � �� $
��Auto ��� $
��Auto �.+��� $��0��$��0��$��Index: ��$
��Bold"��� $��1��$��0��$��3������ *Name+�� $��0��$��1��$��Path: ��$
��Bold"��� $��1��$��1��$��3������ *Path+�� $��2��$��0��$��Number of input files: ��$
��Bold"���  ��Name$ 
cnvSpinner��$��3��$��0��$��9��$��9��$��Center=�$��Right=� J� RenderTransformOrigin$ 0.5,0.5&�$y�Hidden=�z��� trfmSpinner��.+�A� $��0,0,0,0q�$��4��$��4�����	#600076FFA� $��5,0,0,0q�$��4��$��4�����	#A00076FFA� $��5,5,0,0q�$��4��$��4�����	#C00076FFA� $��0,5,0,0q�$��4��$��4�����	#FF0076FF��  ��Name$ tbNumberOfFiles��$��3��$��0��$y�Visible=����� *NumberOfInputFiles+�� $��2��$��1��$��Index size: ��$
��Bold"��� $ tbIndexSize��$��3��$��1��$y�Visible=����� *	IndexSize+�� $ cnvSpinner3��$��3��$��1��$��9��$��9��$��Center=�$��Right=�$ 0.5,0.5&�$y�Hidden=�z��� trfmSpinner3��.+�A� $��0,0,0,0q�$��4��$��4�����	#600076FFA� $��5,0,0,0q�$��4��$��4�����	#A00076FFA� $��5,5,0,0q�$��4��$��4�����	#C00076FFA� $��0,5,0,0q�$��4��$��4�����	#FF0076FF�� $	 imgError��$��0��$��2��$��4��$6��0/CodeXCavator.Indexer;component/Images/error.png��$
��None=�$��5,0,0,0q�
 �� *Error+ � Triggers :�  :� RoutedEvent$ MouseLeftButtonDown��.[��� .���� .����  �� 
TargetName$ 	popErrors��  �� TargetProperty$  IsOpen�p�s� ! �� Value! . " �� KeyTime$" 0��� # �Name$# 	popErrors��$ � AllowsTransparency$ . $
��Fade=�$��Center=�% � PlacementTarget% �� & �� ElementName$& imgError��.���� 
����������	#80000000$��1q�$ 5q�$��	0,0,10,10q�$ 4��' J� Effect' 3  -System.Windows.Media.Effects.DropShadowEffect  (   Opacity$	( 0.5Q�._�]� ��. .+��� $	��Top=�$��0q�$��0,0,0,2q�$
��Bold"�.���� ���� *Name+) �� StringFormat$) Errors: {0}���� $��Bottom=�$ 
Horizontal=�$��RightToLeft=�$��0,5,0,0q�.+��� $��Bottom=�$��ApplicationCommands.Copy��$!
 Copy selection to clipboard��|�.  ��.  ���� $& 	grdErrors��.���� $?��9/CodeXCavator.Indexer;component/Images/clipboard_sign.png��$
��None=� .  . �� $��Bottom=�$#��ApplicationCommands.SelectAll��$
 
Select all��|�.  ��.  ���� $& 	grdErrors��.���� $A��;/CodeXCavator.Indexer;component/Images/selection_select.png��$
��None=� .  . &   System.Windows.Controls.DataGrid  *  Name$* 	grdErrors��$	��Top=�$
��Auto��$	��200��$	��600��+   
IsReadOnly+ . ,   HeadersVisibility$
, None=�-   AutoGenerateColumns- .  .   GridLinesVisibility$
. None=�$ 2��/   AlternatingRowBackground/ �	#20FF0000���� *Errors+0   Columns0 0  *System.Windows.Controls.DataGridTextColumn  1  +System.Windows.Controls.DataGridBoundColumn1   Binding1 ��  :� $ 
MouseLeave��.[��� .���� .���� $  IsOpen�p�s� ! .  $" 0���� $ 5q�$��2q�$ 1��y��� *
IsIndexing+2 �� 	Converter#2 [  ���� $ 0.5,0&�$ 0.5,1&�.��� $ 	#FFFFFFFF��$	 0.5Q�� $ 	#40000000��$ 1Q�._��  �� $
��Auto ��� $��* ��� $
��Auto � �� $
��Auto ��� $
��Auto ��� $
��Auto �.+��� $��0��$��0��$��Indexing progress: ��$
��Bold"��� $��0��$��1��$��2��$��0,0,5,0q����� *CurrentFile+�� $ cnvSpinner2��$��2��$��1��$��9��$��9��$��Center=�$��Right=�$ 0.5,0.5&�$y�Hidden=�z��� trfmSpinner2��.+�A� $��0,0,0,0q�$��4��$��4�����	#600076FFA� $��5,0,0,0q�$��4��$��4�����	#A00076FFA� $��5,5,0,0q�$��4��$��4�����	#C00076FFA� $��0,5,0,0q�$��4��$��4�����	#FF0076FF� 3 �Name$3 pbIndexProgress��$��0��$��2��$��2��$��16��$��0,5,0,0q�4 � Minimum$4 0Q�5 � IsIndeterminate5 .  6 � Maximum6 �� *NumberOfInputFiles+7 � Value7 �� *
Progress+8 �� Mode$8 OneWay=�9 �� Triggers9 �� : �� Binding: �� *NumberOfInputFiles+; �� Value; Z� < N� EnterActions< �� = ��Name$= 
stbSpinner������ > g� RepeatBehavior$> Forever��.��W� $ trfmSpinner��$  Angle�? W� From$? 0X�	@ W� To$	@ 360X�A N� ExitActionsA �� B �� BeginStoryboardName$B 
stbSpinner��a��� C �� 
TargetName$C 
cnvSpinner��D �� PropertyD � y�E �� Value$E Visible=��� $C tbNumberOfFiles��D � y�$E Hidden=��� $; 0��: �� *NumberOfInputFiles+a��� $C pbIndexProgress��D �@�IsIndeterminateE . �� : �� *	IndexSize+; Z� < �� $= stbSpinner3������ $> Forever��.��W� $ trfmSpinner3��$  Angle�$? 0X�$	@ 360X�A �� $B stbSpinner3��a��� $C cnvSpinner3��D � y�$E Visible=��� $C tbIndexSize��D � y�$E Hidden=��� $
; true��: �� *
IsIndexing+< �� $= stbSpinner2������ $> Forever��.��W� $ trfmSpinner2��$  Angle�$? 0X�$	@ 360X�A �� $B stbSpinner2��a��� $C cnvSpinner2��D � y�$E Visible=��� : �� *Error+; Z� a��� $C imgError��D � y�$E 	Collapsed=�< �� ���� .��W� $ imgError��$  Opacity�$	? 0.0X�$	@ 1.0X�F g� AutoReverseF . $> Forever��G g� Duration$G 	0:0:0:0.5D�