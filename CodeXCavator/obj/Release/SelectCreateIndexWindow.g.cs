﻿#pragma checksum "..\..\SelectCreateIndexWindow.xaml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "BF66FEB391C9615CE656A1A8E079E2C881E30D33"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using CodeXCavator;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace CodeXCavator {
    
    
    /// <summary>
    /// SelectCreateIndexWindow
    /// </summary>
    public partial class SelectCreateIndexWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 18 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button OpenMRU1Button;
        
        #line default
        #line hidden
        
        
        #line 19 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button OpenMRU2Button;
        
        #line default
        #line hidden
        
        
        #line 20 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button OpenMRU3Button;
        
        #line default
        #line hidden
        
        
        #line 24 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox XMLIndexFile;
        
        #line default
        #line hidden
        
        
        #line 27 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox XMLIndexLocation;
        
        #line default
        #line hidden
        
        
        #line 30 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox FileSourceDirectories;
        
        #line default
        #line hidden
        
        
        #line 32 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.CheckBox FileSourceDirectoriesRecursive;
        
        #line default
        #line hidden
        
        
        #line 34 "..\..\SelectCreateIndexWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox FileSourceDirectoriesInclude;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/CodeXCavator;component/selectcreateindexwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\SelectCreateIndexWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 17 "..\..\SelectCreateIndexWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.BrowseExistingIndexFile_Click);
            
            #line default
            #line hidden
            return;
            case 2:
            this.OpenMRU1Button = ((System.Windows.Controls.Button)(target));
            
            #line 18 "..\..\SelectCreateIndexWindow.xaml"
            this.OpenMRU1Button.Click += new System.Windows.RoutedEventHandler(this.OpenMRU1Button_Click);
            
            #line default
            #line hidden
            return;
            case 3:
            this.OpenMRU2Button = ((System.Windows.Controls.Button)(target));
            
            #line 19 "..\..\SelectCreateIndexWindow.xaml"
            this.OpenMRU2Button.Click += new System.Windows.RoutedEventHandler(this.OpenMRU2Button_Click);
            
            #line default
            #line hidden
            return;
            case 4:
            this.OpenMRU3Button = ((System.Windows.Controls.Button)(target));
            
            #line 20 "..\..\SelectCreateIndexWindow.xaml"
            this.OpenMRU3Button.Click += new System.Windows.RoutedEventHandler(this.OpenMRU3Button_Click);
            
            #line default
            #line hidden
            return;
            case 5:
            this.XMLIndexFile = ((System.Windows.Controls.TextBox)(target));
            return;
            case 6:
            
            #line 25 "..\..\SelectCreateIndexWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.BrowseNewIndexFile_Click);
            
            #line default
            #line hidden
            return;
            case 7:
            this.XMLIndexLocation = ((System.Windows.Controls.TextBox)(target));
            return;
            case 8:
            
            #line 28 "..\..\SelectCreateIndexWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.BrowseIndexContent_Click);
            
            #line default
            #line hidden
            return;
            case 9:
            this.FileSourceDirectories = ((System.Windows.Controls.TextBox)(target));
            return;
            case 10:
            
            #line 31 "..\..\SelectCreateIndexWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.BrowseCodeDirs_Click);
            
            #line default
            #line hidden
            return;
            case 11:
            this.FileSourceDirectoriesRecursive = ((System.Windows.Controls.CheckBox)(target));
            return;
            case 12:
            this.FileSourceDirectoriesInclude = ((System.Windows.Controls.TextBox)(target));
            return;
            case 13:
            
            #line 35 "..\..\SelectCreateIndexWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.CreateIndexFile_Click);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}

