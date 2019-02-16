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
using CodeXCavator.Engine.Interfaces;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace CodeXCavator.Engine
{
    /// <summary>
    /// FileAction class.
    /// 
    /// Generic implementation of the file action class.
    /// </summary>
    public class FileAction : IFileAction
    {
        Func<string, bool> mCanExecute;
        Action<string> mExecute;

        public FileAction( string name, string caption, string description, Func<string,bool> canExecute, Action<string> execute )
        {
            Name = name;
            Caption = caption;
            Description = description;
            mCanExecute = canExecute;
            mExecute = execute;
        }

        public string Name
        {
            get;
            private set;
        }

        public string Caption
        {
            get;
            private set;
        }

        public string Description
        {
            get;
            private set;
        }

        public virtual bool CanExecute( string filePath )
        {
            if( mCanExecute != null )
                return mCanExecute( filePath );
            return false;
        }

        public virtual void Execute( string filePath )
        {
            if( mExecute != null )
                mExecute( filePath );
        }
    }

    /// <summary>
    /// FileActions class.
    /// 
    /// The file actions class is a registry for file actions.
    /// </summary>
    public static class FileActions
    {
        private static HashSet<IFileAction> sRegisteredFileActions = new HashSet<IFileAction>();

        /// <summary>
        /// Static constructor.
        /// </summary>
        static FileActions()
        {
            RegisterDefaultFileActions();
        }

        /// <summary>
        /// Registers all default file actions.
        /// </summary>
        private static void RegisterDefaultFileActions()
        {
            RegisterFileAction( "FileActionCopyFullFilePathToClipboard", "Copy full path to clipboard", "Copies the full path of the file to the clipboard.", CanCopyFullFilePathToClipboard, CopyFullFilePathToClipboard );
            RegisterFileAction( "FileActionCopyFileNameToClipboard", "Copy file name to clipboard", "Copies the file name to the clipboard.", CanCopyFileNameToClipboard, CopyFileNameToClipboard );
            RegisterFileAction( "FileActionCopyDirectoryToClipboard", "Copy directory to clipboard", "Copies the directory to the clipboard.", CanCopyDirectoryToClipboard, CopyDirectoryToClipboard ); 
            RegisterFileAction( "FileActionOpenContainingFolder", "Open containing folder", "Opens the folder containing the file.", CanOpenFolderContainingFile, OpenFolderContainingFile );
            RegisterFileAction( "FileActionOpenFileWithExternalEditor", "Open file with external editor", "Opens the file with an external editor registered in the operating system.", CanOpenFileWithExternalEditor, OpenFileWithExternalEditor );
            RegisterFileAction( "FileActionOpenFileWithExternalEditorAsAdministrator", "Open file with external editor as administrator", "Opens the file with an external editor registered in the operating system as administrator.", CanOpenFileWithExternalEditorAsAdministrator, OpenFileWithExternalEditorAsAdministrator );

            PluginManager.LoadPlugins<Interfaces.IFileAction>();
        }

        /// <summary>
        /// Checks, whether the file path can be copied into clipboard.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>True, if file path is not empty.</returns>
        private static bool CanCopyFullFilePathToClipboard( string filePath )
        {
            return ( !string.IsNullOrEmpty( filePath ) );
        }

        /// <summary>
        /// Copies the full file path into the clipboard.
        /// </summary>
        /// <param name="filePath">File path, which should be copied into the clipboard.</param>
        private static void CopyFullFilePathToClipboard( string filePath )
        {
            if( CanCopyFullFilePathToClipboard( filePath ) )
                System.Windows.Clipboard.SetText( filePath );
        }

        /// <summary>
        /// Checks, whether the file name can be copied into clipboard.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns>True, if file path is not empty.</returns>
        private static bool CanCopyFileNameToClipboard( string filePath )
        {
            return ( !string.IsNullOrEmpty( filePath ) );
        }

        /// <summary>
        /// Copies the file name into the clipboard.
        /// </summary>
        /// <param name="filePath">File path, which should be copied into the clipboard.</param>
        private static void CopyFileNameToClipboard( string filePath )
        {
            if( CanCopyFileNameToClipboard( filePath ) )
                System.Windows.Clipboard.SetText( Path.GetFileName( filePath ) );
        }

        /// <summary>
        /// Checks, whether the directory can be copied into clipboard.
        /// </summary>
        /// <param name="filePath">File path, whose base directory should be copied to clipboard.</param>
        /// <returns>True, if file path is not empty.</returns>
        private static bool CanCopyDirectoryToClipboard( string filePath )
        {
            return ( !string.IsNullOrEmpty( filePath ) );
        }

        /// <summary>
        /// Copies the directory into the clipboard.
        /// </summary>
        /// <param name="filePath">File path, whose base directory should be copied to clipboard.</param>
        private static void CopyDirectoryToClipboard( string filePath )
        {
            if( CanCopyDirectoryToClipboard( filePath ) )
                System.Windows.Clipboard.SetText( Path.GetDirectoryName( filePath ) );
        }
        /// <summary>
        /// Returns, whether the folder containing the specified can be opened.
        /// </summary>
        /// <param name="filePath">Path of the file, for which the containing folder should be opened.</param>
        /// <returns>True, if the folder containing the specified file can be openend, false otherwise.</returns>
        private static bool CanOpenFolderContainingFile( string filePath )
        {
            if( !string.IsNullOrEmpty( filePath ) )
            {
                string containingDirectory = Path.GetDirectoryName( filePath );
                return Directory.Exists( containingDirectory );
            }
            return false;
        }

        /// <summary>
        /// Opens the folder containing the specified file.
        /// </summary>
        /// <param name="filePath">Path of the file, whose containing folder should be opened.</param>
        private static void OpenFolderContainingFile( string filePath )
        {
            if( CanOpenFolderContainingFile( filePath ) )
            {
                if( System.IO.File.Exists( filePath ) )
                {
                    System.Diagnostics.Process.Start( "explorer.exe", string.Format( "/select,\"{0}\"", filePath ) );
                }
                else
                {
                    string containingDirectory = Path.GetDirectoryName( filePath );
                    System.Diagnostics.Process.Start( "explorer.exe", string.Format( "\"{0}\"", containingDirectory ) );
                }
            }
        }

        /// <summary>
        /// Returns, whether the file can be opened with an external editor.
        /// </summary>
        /// <param name="filePath">Path of the file, for which the containing folder should be opened.</param>
        /// <returns>True, if the folder containing the specified file can be openend, false otherwise.</returns>
        private static bool CanOpenFileWithExternalEditor( string filePath )
        {
            if( !string.IsNullOrEmpty( filePath ) )
                return File.Exists( filePath );
            return false;
        }

        /// <summary>
        /// Opens the file with the external editor registered by the operating system.
        /// </summary>
        /// <param name="filePath">Path of the file, whose containing folder should be opened.</param>
        private static void OpenFileWithExternalEditor( string filePath )
        {
            if( CanOpenFileWithExternalEditor( filePath ) )
            {
                if( System.IO.File.Exists( filePath ) )
                    System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo { FileName = filePath, UseShellExecute = true } );
            }
        }

        [DllImport( "shell32.dll", EntryPoint = "IsUserAnAdmin" )]
        private static extern bool IsUserAnAdministrator();

        /// <summary>
        /// Determines the default system editor for the given file path.
        /// </summary>
        /// <param name="filePath">File path.</param>
        /// <returns>Path to the default editor associated with the specified file path.</returns>
        private static string GetDefaultShellOpenCommand( string filePath )
        {
            if( string.IsNullOrEmpty( filePath ) )
                return string.Empty;

            string extension = Path.GetExtension( filePath );
            if( string.IsNullOrEmpty( extension ) )
                return string.Empty;

            try
            {
                using( var extensionKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey( extension ) )
                {
                    string association = extensionKey.GetValue( "" ) as string;
                    if( association != null )
                    {
                        using( var commandKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey( association + @"\Shell\Open\Command" ) )
                            return commandKey.GetValue( "" ) as string;
                    }
                }

            }
            catch
            {
            }

            return string.Empty;
        }

        /// <summary>
        /// Returns, whether the file can be opened with an external editor.
        /// </summary>
        /// <param name="filePath">Path of the file, for which the containing folder should be opened.</param>
        /// <returns>True, if the folder containing the specified file can be openend, false otherwise.</returns>
        private static bool CanOpenFileWithExternalEditorAsAdministrator( string filePath )
        {
            if( !string.IsNullOrEmpty( filePath ) )
            {
                return File.Exists( filePath ) && !IsUserAnAdministrator() && !string.IsNullOrEmpty( GetDefaultShellOpenCommand( filePath ) );
            }
            return false;
        }

        private enum CommandScannerState
        {
            Init,
            App,
            AppQuoted
        }

        /// <summary>
        /// Splits a command string into an application and arguments part.
        /// </summary>
        /// <param name="command">Command string.</param>
        /// <param name="app">Application part.</param>
        /// <param name="arguments">Arguments part.</param>
        private static void SplitCommandIntoApplicationAndArguments( string command, out string app, out string arguments )
        {
            app = null;
            arguments = null;
            CommandScannerState state = CommandScannerState.Init;
            int appStart = -1;
            for( int c = 0 ; c < command.Length ; ++c )
            {
                char currentChar = command[c];
                switch( state )
                {
                    case CommandScannerState.Init:
                        {
                            if( currentChar == '\"' )
                            {
                                state = CommandScannerState.AppQuoted;
                                appStart = c;
                            }
                            else
                            if( !Char.IsWhiteSpace( currentChar ) )
                            {
                                state = CommandScannerState.App;
                                appStart = c;
                            }
                        }
                        break;
                    case CommandScannerState.App:
                        {
                            if( Char.IsWhiteSpace( currentChar ) )
                            {
                                app = command.Substring( appStart, c - appStart ).Trim();
                                arguments = command.Substring( c + 1 ).Trim();
                                return;
                            }
                        }
                        break;
                    case CommandScannerState.AppQuoted:
                        {
                            if( currentChar == '\"' )
                            {
                                app = command.Substring( appStart, c - appStart + 1 ).Trim();
                                arguments = command.Substring( c + 1) .Trim();
                                return;
                            }
                        }
                        break;
                }
            }
            if( appStart >= 0 )
            {
                app = command.Substring( appStart );
                arguments = string.Empty;
            }
        }

        /// <summary>
        /// Opens the file with the external editor registered by the operating system.
        /// </summary>
        /// <param name="filePath">Path of the file, whose containing folder should be opened.</param>
        private static void OpenFileWithExternalEditorAsAdministrator( string filePath )
        {
            if( CanOpenFileWithExternalEditor( filePath ) )
            {
                var defaultCommand = GetDefaultShellOpenCommand( filePath );
                string app, arguments;
                SplitCommandIntoApplicationAndArguments( defaultCommand, out app, out arguments );
                if( arguments.Contains( "%1" ) )
                    arguments = arguments.Replace( "%1", filePath );
                else
                    arguments = arguments + " " + filePath;
                if( !string.IsNullOrEmpty( app ) )
                {
                    try
                    {
                        System.Diagnostics.Process.Start( new System.Diagnostics.ProcessStartInfo { FileName = app, Arguments = arguments, UseShellExecute = true, Verb = "runas" } );
                    }
                    catch( Win32Exception )
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Registers a file action.
        /// </summary>
        /// <typeparam name="FILE_ACTION_TYPE">Type of the file action to be registered.</typeparam>
        public static void RegisterFileAction<FILE_ACTION_TYPE>() where FILE_ACTION_TYPE : IFileAction, new()
        {
            RegisterFileAction( new FILE_ACTION_TYPE() );
        }

        /// <summary>
        /// Registers a file action.
        /// </summary>
        /// <param name="actionType">Type of the action to be registered. The type must have a public constructor and must derive from IFileAction.</param>
        public static void RegisterFileAction( Type actionType )
        {
            if( actionType != null && typeof( IFileAction ).IsAssignableFrom( actionType ) )
            {
                try
                {
                    IFileAction fileAction = (IFileAction) Activator.CreateInstance( actionType );
                    RegisterFileAction( fileAction );
                }
                catch
                {
                }
            }
        }

        /// <summary>
        /// Registers a file action.
        /// </summary>
        /// <param name="fileAction">File action to be registered.</param>
        public static void RegisterFileAction( IFileAction fileAction )
        {
            if( fileAction != null )
            {
                sRegisteredFileActions.Add( fileAction );
            }
        }

        /// <summary>
        /// Registers an action.
        /// </summary>
        /// <param name="name">Name of the action, which should be registered.</param>
        /// <param name="caption">Caption of the action.</param>
        /// <param name="description">Description of the action.</param>
        /// <param name="canExecute">Delegate, which determines, whether the action can be executed for a certain file.</param>
        /// <param name="execute">Delegate, which executes the action.</param>
        public static void RegisterFileAction( string name, string caption, string description, Func<string, bool> canExecute, Action<string> execute )
        {
            RegisterFileAction( new FileAction( name, caption, description, canExecute, execute ) );
        }

        /// <summary>
        /// Unregisters the specified file action.
        /// </summary>
        /// <param name="fileAction">File action, which should be unregistered.</param>
        public static void UnregisterFileAction( IFileAction fileAction )
        {
            if( fileAction != null )
            {
                sRegisteredFileActions.Remove( fileAction );
            }
        }

        /// <summary>
        /// Unregisters all file actions with the specified name.
        /// </summary>
        /// <param name="name">Name of the action, which should be unregistered.</param>
        public static void UnregisterFileAction( string name )
        {
            sRegisteredFileActions.RemoveWhere( action => action.Name.Equals( name ) );
        }

        /// <summary>
        /// Unregisters all file actions.
        /// </summary>
        public static void UnregsiterAllFileActions()
        {
            sRegisteredFileActions.Clear();
        }

        /// <summary>
        /// Returns the file action with the specified name.
        /// </summary>
        /// <param name="name">Name of the action, which should be returned.</param>
        /// <returns>First action, which matches the the specified name, or null, if no file action can be found.</returns>
        public static IFileAction FindRegisteredFileActionWithName( string name )
        {
            return FindRegisteredFileActionsWithName( name ).FirstOrDefault();
        }

        /// <summary>
        /// Returns all file actions, which match the specified name.
        /// </summary>
        /// <param name="name">Name of the actions, which should be returned.</param>
        /// <returns>Enumeration of all registered actions, matching the specified name.</returns>
        public static IEnumerable<IFileAction> FindRegisteredFileActionsWithName( string name )
        {
            return sRegisteredFileActions.Where( fileAction => fileAction.Name.Equals( name ) );
        }

        /// <summary>
        /// Returns all registered file actions.
        /// </summary>
        public static IEnumerable<IFileAction> RegisteredFileActions
        {
            get
            {
                return sRegisteredFileActions;
            }
        }

        /// <summary>
        /// Returns all executable actions for the specified file.
        /// </summary>
        /// <param name="filePath">Path of the file, for which all executable actions should be returned.</param>
        /// <returns></returns>
        public static IEnumerable<IFileAction> GetExecutableActionsForFile( string filePath )
        {
            return sRegisteredFileActions.Where( fileAction => fileAction.CanExecute( filePath ) );
        }
    }
}
