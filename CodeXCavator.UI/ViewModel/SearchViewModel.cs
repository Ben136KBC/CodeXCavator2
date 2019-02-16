using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CodeXCavator.Engine.Interfaces;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;

//This file has been modified by Ben van der Merwe

namespace CodeXCavator.UI
{
    /// <summary>
    /// Handles most recently used (MRU) files
    /// </summary>
    public static class MRUHandler
    {
        static public event EventHandler OpenCreateIndexViewMethod;
        public static void OnOpenCreateIndexViewMethod()
        {
            if (OpenCreateIndexViewMethod != null) OpenCreateIndexViewMethod(null, EventArgs.Empty);
        }

        /// <summary>
        /// Native call to add the file to windows' recent file list
        /// </summary>
        /// <param name="uFlags">Always use (uint)ShellAddRecentDocs.SHARD_PATHW</param>
        /// <param name="pv">path to file</param>
        enum ShellAddRecentDocs
        {
            SHARD_PIDL = 0x00000001,
            SHARD_PATHA = 0x00000002,
            SHARD_PATHW = 0x00000003
        }
        [DllImport("shell32.dll")]
        public static extern void SHAddToRecentDocs(UInt32 uFlags, [MarshalAs(UnmanagedType.LPWStr)] String pv);

        // The separator character is used to separate multiple files in the registry mruFiles string.
        // Use the pipe | character because it is not legal in file names.
        public static string separatorString = "|";

        static void AddFileToWindowsRecentFilesList(string fileName)
        {
            try
            {
                SHAddToRecentDocs((uint)ShellAddRecentDocs.SHARD_PATHW, fileName);
            }
            catch
            {
            }
        }

        public static string Replace(this string str, string old, string @new, StringComparison comparison)
        {
            @new = @new ?? "";
            if (string.IsNullOrEmpty(str) || string.IsNullOrEmpty(old) || old.Equals(@new, comparison))
                return str;
            int foundAt = 0;
            while ((foundAt = str.IndexOf(old, foundAt, comparison)) != -1)
            {
                str = str.Remove(foundAt, old.Length).Insert(foundAt, @new);
                foundAt += @new.Length;
            }
            return str;
        }

        /// <summary>
        /// Add the given file to the MRU list, pushing it to the front.
        /// The file may or may not be in the MRU already.
        /// </summary>
        public static void PushMRUFile(string lastFile)
        {
            var userSettings = new UI.UserSettings();
            new Engine.RegistryUserSettingsStorageProvider().Restore("CodeXCavator", userSettings);
            string mruFiles = userSettings.MRUFiles;
            if (mruFiles == null)
            {
                mruFiles = "";
            }
            //Also push it to the Windows recent files list, to make it easier for users to find.
            //We dont retrieve files from there, because XML files are generic and could be anything.
            AddFileToWindowsRecentFilesList(lastFile);

            string mruFile = lastFile + separatorString;
            if (mruFiles.ToLower().StartsWith(lastFile.ToLower()))
            {
                //Nothing to do
            }
            else
            {
                //Put the new file at the front
                mruFiles = Replace(mruFiles, mruFile, "", StringComparison.OrdinalIgnoreCase);
                string newmruFiles = mruFile + mruFiles;
                userSettings.MRUFiles = newmruFiles;
                //And save it back out to the registry
                new Engine.RegistryUserSettingsStorageProvider().Store("CodeXCavator", userSettings);
            }
        }

        /// <summary>
        /// Get the number of MRU files
        /// </summary>
        public static int GetMRUSize()
        {
            var userSettings = new UI.UserSettings();
            new Engine.RegistryUserSettingsStorageProvider().Restore("CodeXCavator", userSettings);
            string mruFiles = userSettings.MRUFiles;
            int cnt = 0;
            if (mruFiles != null && mruFiles != "")
            {
                string[] delimiters = new string[] { separatorString };
                string[] files = mruFiles.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                cnt = files.Length;
            }
            return cnt;
        }

        /// <summary>
        /// Get an MRU file, whichMru starts at 0 for the first, most recent, file.
        /// </summary>
        public static string GetLastMRUFile(int whichMru)
        {
            string mruFile = "";

            var userSettings = new UI.UserSettings();
            new Engine.RegistryUserSettingsStorageProvider().Restore("CodeXCavator", userSettings);
            string mruFiles = userSettings.MRUFiles;
            if (mruFiles != null && mruFiles != "")
            {
                string[] delimiters = new string[] { separatorString };
                string[] files = mruFiles.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                if (whichMru >= 0 && whichMru < files.Length)
                {
                    mruFile = files[whichMru];
                }
            }
            return mruFile;
        }

        /// <summary>
        /// Find the index xml file which contains the Path in the Index section.
        /// </summary>
        public static string GetFileWithPath(string path)
        {
            string file = "";
            int mruSize = GetMRUSize();
            for (int i = 0; i < mruSize; i++)
            {
                string xmlFile = GetLastMRUFile(i);
                if (xmlFile != "")
                {
                    try
                    {
                        var stream = new System.IO.FileStream(xmlFile, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
                        try
                        {
                            System.Xml.Linq.XElement root = System.Xml.Linq.XDocument.Load(stream).Root;
                            if (root != null && root.Name.LocalName == "Index")
                            {
                                // Get index path from XML attribute
                                System.Xml.Linq.XAttribute pathAttribute = root.Attribute("Path");
                                if (pathAttribute != null && pathAttribute.Value != null && pathAttribute.Value == path)
                                {
                                    file = xmlFile;
                                    stream.Close();
                                    break;
                                }
                            }
                        }
                        catch
                        {
                        }
                        stream.Close();
                    }
                    catch
                    {
                    }
                }
            }
            return file;
        }

        /// <summary>
        /// Given an index file, run the indexer to update it.
        /// </summary>
        public static bool UpdateIndexFile(string indexFile)
        {
            bool ok = false;
            try
            {
                //Now build the index file, wait for the index to exit.
                string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
                System.Diagnostics.ProcessStartInfo pInfo = new System.Diagnostics.ProcessStartInfo();
                //Switch to the Deploy directory if need be, while debugging.
                appDirectory = appDirectory.Replace("\\CodeXCavator\\bin\\Debug", "\\Deploy");
                appDirectory = appDirectory.Replace("\\CodeXCavator\\bin\\Release", "\\Deploy");
                pInfo.FileName = appDirectory + "CodeXCavator.Indexer.exe";
                string args = "-autoclose \"" + indexFile + "\"";
                pInfo.Arguments = args;
                System.Diagnostics.Process p = System.Diagnostics.Process.Start(pInfo);
                p.WaitForInputIdle();
                p.WaitForExit();
                ok = true;
            }
            catch (Exception ex)
            {
                //Show if the new file contains an invalid path etc.
                MessageBox.Show(ex.Message);
            }
            return ok;
        }
    }
}

namespace CodeXCavator.UI.ViewModel
{
    internal class SearchViewModel : ViewModelBase
    {
        UserSettings mUserSettings;
        private IndexViewModel mCurrentIndex;
        private IIndexSearcher mCurrentSearcher;

        private ObservableCollection<DirectoryFilterEntryViewModel> mDirectories;

        private DirectoryFilter mDirectoryFilter;

        private IEnumerable<FileTypeFilterItemViewModel> mFileTypes;
        private FileTypeFilter mFileTypeFilter;
        private bool mEnableAllFileTypes;

        private bool mIsFilterByFileListEnabled;
        private FileViewModel mSelectedFile;

        private SearchProcessor mContentsSearchProcessor = new SearchProcessor( SearchType.Contents, true, true, true );
        private SearchProcessor mTagSearchProcessor = new SearchProcessor( SearchType.Tags, true, true, true );
        private SearchProcessor mFileSearchProcessor = new SearchProcessor( SearchType.Files, false, false, false );
        private SearchProcessor mCurrentSearchProcessor;
        private int mCurrentSearchProcessorIndex;

        internal SearchViewModel( IEnumerable<IIndex> indexes, UserSettings userSettings = null )
        {
            mUserSettings = userSettings;
            InitializeCurrentIndexFromUserSettings( indexes, userSettings );
            InitializeDirectoryFilter();
            InitializeFileTypeFilter();
            InitializeSearchProcessors( userSettings );
        }

        private void InitializeDirectoryFilter()
        {
            mDirectories = new ObservableCollection<DirectoryFilterEntryViewModel>();
            mDirectoryFilter = new DirectoryFilter( mDirectories );
        }

        private void InitializeFileTypeFilter()
        {
            mFileTypeFilter = new FileTypeFilter();
            mFileTypes = mCurrentIndex != null ? mCurrentIndex.FileTypes.Select( fileType => new FileTypeFilterItemViewModel( mFileTypeFilter, fileType ) ).ToArray() : new FileTypeFilterItemViewModel[] { };
            mEnableAllFileTypes = DetermineIfAllFileTypesAreEnabled( mFileTypes );
        }

        /// <summary>
        /// Sets up all search processors.
        /// </summary>
        private void InitializeSearchProcessors( UserSettings userSettings )
        {
            SetupSearchProcessor( ContentsSearchProcessor, userSettings );
            SetupSearchProcessor( FileSearchProcessor, userSettings );
            SetupSearchProcessor( TagSearchProcessor, userSettings );

            FileSearchProcessor.PropertyChanged += FileSearchProcessor_PropertyChanged;
            CurrentSearchProcessor = mContentsSearchProcessor;
        }

        /// <summary>
        /// Setups the specified search processor
        /// </summary>
        /// <param name="searchProcessor">Search processor to be setup.</param>
        private void SetupSearchProcessor( SearchProcessor searchProcessor, UserSettings userSettings )
        {
            searchProcessor.SearchQuery = null;
            searchProcessor.Index = mCurrentIndex;
            searchProcessor.Searcher = mCurrentSearcher;
            searchProcessor.DirectoryFilter = mDirectoryFilter;
            searchProcessor.FileTypeFilter = mFileTypeFilter;

            //Default to being case insensitive, if there is no setting.
            string searchOption = "CaseSensitive";
            searchProcessor.SetSearchOption(searchOption, false);

            if ( userSettings != null )
                InitializeSearchProcessorFromUserSettings( userSettings, searchProcessor );

            searchProcessor.PropertyChanged += SearchProcessorPropertyChanged;
        }

        private void InitializeCurrentIndexFromUserSettings( IEnumerable<IIndex> indexes, UserSettings userSettings )
        {
            if( indexes != null )
            {
                // Load current index from user settings...
                if( userSettings != null )
                {
                    var lastIndexUsed = indexes.FirstOrDefault( index => index.IndexPath.Equals( userSettings.LastSelectedIndex, StringComparison.OrdinalIgnoreCase ) );
                    mCurrentIndex = lastIndexUsed != null ? new IndexViewModel( lastIndexUsed ) : null;
                }

                // Current index not set? Use first index in list.
                if( mCurrentIndex == null )
                    mCurrentIndex = indexes.Any() ? new IndexViewModel( indexes.First() ) : null;

                // Update user settings.
                if( userSettings != null && mCurrentIndex != null )
                    userSettings.LastSelectedIndex = mCurrentIndex.Index.IndexPath;
            }

            mCurrentSearcher = mCurrentIndex != null ? mCurrentIndex.Index.CreateSearcher() : null;
        }


        /// <summary>
        /// Initializes a search processor from user settings.
        /// </summary>
        /// <param name="userSettings">User settings, from which the search processor should be initialized.</param>
        /// <param name="searchProcessor">Search processor, which should be initialized.</param>
        private static void InitializeSearchProcessorFromUserSettings( UserSettings userSettings, SearchProcessor searchProcessor )
        {
            if( userSettings == null )
                return;
            UserSettings.SearchProcessorSettings searchProcessorSettings = null;
            if( userSettings.SearchProcessors.TryGetValue( searchProcessor.SearchType, out searchProcessorSettings ) )
            {
                searchProcessor.ImmediateSearch = searchProcessorSettings.ImmediateSearch;
                InitializeSearchOptionsFromOptionEntries( searchProcessorSettings.SearchOptions, searchProcessor.SearchOptions );
            }

        }

        /// <summary>
        /// Initializes search options from option entries.
        /// </summary>
        /// <param name="optionEntries">Option entries.</param>
        /// <param name="optionsProvider">Options provider of which the options should be set.</param>
        private static void InitializeSearchOptionsFromOptionEntries( IEnumerable<KeyValuePair<string, object>> optionEntries, IOptionsProvider optionsProvider )
        {
            if( optionEntries != null && optionsProvider != null )
            {
                foreach( var searchOption in optionEntries.ToArray() )
                    optionsProvider.SetOptionValue( searchOption.Key, searchOption.Value );
            }
        } 

        /// <summary>
        /// Handles the change of a search processor property.
        /// 
        /// This method stores the configuration of the search processor in the user settings.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event arguments.</param>
        void SearchProcessorPropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            const string SEARCH_OPTIONS_PREFIX = "SearchOptions.";
            var searchProcessor = sender as SearchProcessor;
            if( searchProcessor != null )
            {
                UserSettings.SearchProcessorSettings searchProcessorSettings = null;
                if( mUserSettings.SearchProcessors.TryGetValue( searchProcessor.SearchType, out searchProcessorSettings ) )
                {
                    if( e.PropertyName.StartsWith( SEARCH_OPTIONS_PREFIX ) )
                    {
                        string searchOption = e.PropertyName.Substring( SEARCH_OPTIONS_PREFIX.Length );
                        searchProcessorSettings.SearchOptions[searchOption] = searchProcessor.SearchOptions.GetOptionValue( searchOption );
                    }
                    else
                    {
                        var processorProperty = searchProcessor.GetType().GetProperty( e.PropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty );
                        var settingsProperty = searchProcessorSettings.GetType().GetProperty( e.PropertyName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.SetProperty );
                        if( settingsProperty != null && settingsProperty.CanWrite && settingsProperty.GetSetMethod() != null && processorProperty != null )
                            settingsProperty.SetValue( searchProcessorSettings, processorProperty.GetValue( searchProcessor, null ), null );
                    }
                }
            }
        }

        /// <summary>
        /// Handles file search processor property changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void FileSearchProcessor_PropertyChanged( object sender, PropertyChangedEventArgs e )
        {
            // Check properties, on which ShowFileSearchInfo property depends...
            if( e.PropertyName == "IsSearching" ||
                e.PropertyName == "SearchResultCount" )
                OnPropertyChanged( "ShowFileSearchInfo" );

            // Check properties, on which Files property depends...
            if( e.PropertyName == "SearchResults" )
                OnPropertyChanged( "Files" );
        }

        /// <summary>
        /// View model for the current index on which search is performed.
        /// </summary>
        public IndexViewModel CurrentIndex
        {
            get
            {
                return mCurrentIndex;
            }
            set
            {
                if( mCurrentIndex != value )
                {
                    mCurrentIndex = value;
                    mCurrentSearcher = value != null ? value.Index.CreateSearcher() : null;

                    var searchQuery = mContentsSearchProcessor.SearchQuery;
                    InitializeSearchProcessors( mUserSettings );
                    mContentsSearchProcessor.SearchQuery = searchQuery;

                    mFileTypeFilter.Reset();
                    mFileTypes = mCurrentIndex != null ? mCurrentIndex.FileTypes.Select( fileType => new FileTypeFilterItemViewModel( mFileTypeFilter, fileType ) ).ToArray() : new FileTypeFilterItemViewModel[] { };

                    if( mUserSettings != null )
                        mUserSettings.LastSelectedIndex = mCurrentIndex != null ? mCurrentIndex.Index.IndexPath : null;

                    OnPropertyChanged( "CurrentIndex", "FileTypes", "Files" );
                }
            }

        }

        /// <summary>
        /// Search processor used for searching file contents.
        /// </summary>
        public SearchProcessor ContentsSearchProcessor
        {
            get { return mContentsSearchProcessor; }
        }

        /// <summary>
        /// Search processor used for searching tags.
        /// </summary>
        public SearchProcessor TagSearchProcessor
        {
            get { return mTagSearchProcessor; }
        }

        public int CurrentSearchProcessorIndex
        {
            get { return mCurrentSearchProcessorIndex; }
            set
            {
                if( mCurrentSearchProcessorIndex != value )
                {
                    mCurrentSearchProcessorIndex = value;
                    OnPropertyChanged( "CurrentSearchProcessorIndex" );

                    switch( mCurrentSearchProcessorIndex )
                    {
                        case 0:
                            CurrentSearchProcessor = mContentsSearchProcessor;
                            break;
                        case 1:
                            CurrentSearchProcessor = mTagSearchProcessor;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Current search processor.
        /// </summary>
        public SearchProcessor CurrentSearchProcessor
        {
            get
            {
                return mCurrentSearchProcessor;
            }
            internal set
            {
                if( mCurrentSearchProcessor != value )
                {
                    mCurrentSearchProcessor = value;
                    OnPropertyChanged( "CurrentSearchProcessor" );
                }
            }
        }
        /// <summary>
        /// Returns the list of files contained in the current index, or the file search result list
        /// </summary>
        public IEnumerable<FileViewModel> Files
        {
            get
            {
                if( mFileSearchProcessor.SearchResults != null )
                    return mFileSearchProcessor.SearchResults.Cast<ViewModel.SearchHitViewModel>().Select( filePath => new ViewModel.FileViewModel( filePath.FilePath, FileViewModel.GetFormattedFilePath( filePath ) ) );
                return CurrentIndex != null ? CurrentIndex.Files.Select( filePath => new ViewModel.FileViewModel( filePath ) ) : null;
            }
        }

        /// <summary>
        /// File types contained in the current index.
        /// </summary>
        public IEnumerable<FileTypeFilterItemViewModel> FileTypes
        {
            get
            {
                return mFileTypes;
            }
            set
            {
                if( mFileTypes != value )
                {
                    mFileTypes = value;
                    mEnableAllFileTypes = DetermineIfAllFileTypesAreEnabled( mFileTypes );
                    OnPropertyChanged( "FileTypes" );
                    OnPropertyChanged( "EnableAllFileTypes" );
                }
            }
        }

        /// <summary>
        /// Determines, whether all file types in the specified list are enabled.
        /// </summary>
        /// <param name="mFileTypes">Enumeration of file type view models.</param>
        /// <returns>True, if all file types in the enumeration are enabled, false otherwise.</returns>
        private bool DetermineIfAllFileTypesAreEnabled( IEnumerable<FileTypeFilterItemViewModel> mFileTypes )
        {
            if( mFileTypes != null )
            {
                foreach( var fileType in mFileTypes )
                {
                    if( !fileType.Enabled )
                        return false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines, whether all file types should be enabled or not.
        /// </summary>
        public bool EnableAllFileTypes
        {
            get { return mEnableAllFileTypes; }
            set
            {
                if( mEnableAllFileTypes != value )
                {
                    mEnableAllFileTypes = value;
                    if( mFileTypes != null )
                    {
                        foreach( var fileType in mFileTypes )
                        {
                            fileType.Enabled = value;
                        }
                    }
                    OnPropertyChanged( "EnableAllFileTypes" );
                }
            }
        }

        /// <summary>
        /// Search processor used for searching files.
        /// </summary>
        public SearchProcessor FileSearchProcessor
        {
            get { return mFileSearchProcessor; }
        }

        /// <summary>
        /// Signalizes, whether the file search info should be displayed
        /// </summary>
        public bool ShowFileSearchInfo
        {
            get
            {
                return mFileSearchProcessor.IsSearching || mFileSearchProcessor.SearchResultCount.HasValue;
            }
        }

        /// <summary>
        /// Directory filter entry view models.
        /// </summary>
        public ObservableCollection<DirectoryFilterEntryViewModel> Directories
        {
            get
            {
                return mDirectories;
            }
        }

        /// <summary>
        /// Selected file
        /// </summary>
        public FileViewModel SelectedFile
        {
            get
            {
                return mSelectedFile;
            }
            set
            {
                if( mSelectedFile != value )
                {
                    mSelectedFile = value;
                    OnPropertyChanged( "SelectedFile" );
                }
            }
        }

        private ICommand mUpdateIndexCommand;
        private ICommand mOpenCreateIndexCommand;

        /// <summary>
        /// Update Index command.
        /// </summary>
        public ICommand UpdateIndexCommand
        {
            get
            {
                if (mUpdateIndexCommand == null)
                    mUpdateIndexCommand = new Command(CanUpdate, UpdateIndex);
                return mUpdateIndexCommand;
            }
        }

        /// <summary>
        /// Open or create Index command.
        /// </summary>
        public ICommand OpenCreateIndexCommand
        {
            get
            {
                if (mOpenCreateIndexCommand == null)
                    mOpenCreateIndexCommand = new Command(CanUpdate, OpenCreateIndex);
                return mOpenCreateIndexCommand;
            }
        }

        private bool CanUpdate(object param)
        {
            return true;
        }

        //Ben
        /// <summary>
        /// Open or create an index.
        /// </summary>
        /// <param name="param">Command parameter.</param>
        private void OpenCreateIndex(object param)
        {
            CodeXCavator.UI.MRUHandler.OnOpenCreateIndexViewMethod();
        }

        /// <summary>
        /// Update current index.
        /// </summary>
        /// <param name="param">Command parameter.</param>
        private void UpdateIndex(object param)
        {
            //Now figure out which index file to update.
            string path = CurrentIndex.Path;
            string indexFile = CodeXCavator.UI.MRUHandler.GetFileWithPath(path);
            if (indexFile != "")
            {
                if (CodeXCavator.UI.MRUHandler.UpdateIndexFile(indexFile))
                {
                    //Index updated successfully, now refresh the search routines.
                    OnPropertyChanged("CurrentIndex", "FileTypes", "Files");
                }
            }
            else
            {
                MessageBox.Show(string.Format("Could not determine the xml file for the current index of \"{0}\"!", path), "CodeXCavator - Searcher...", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        /// <summary>
        /// Sets or checks, whether filtering by file type is enabled.
        /// </summary>
        public bool IsFilterByFileTypeEnabled
        {
            get { return mFileTypeFilter.IsEnabled; }
            set
            {
                if( mFileTypeFilter.IsEnabled != value )
                {
                    mFileTypeFilter.IsEnabled = value;
                    OnPropertyChanged( "IsFilterByFileTypeEnabled" );
                }
            }

        }

        /// <summary>
        /// Sets or checks, whether filter by directories is enabled.
        /// </summary>
        public bool IsFilterByDirectoriesEnabled
        {
            get { return mDirectoryFilter.IsEnabled; }
            set
            {
                if( mDirectoryFilter.IsEnabled != value )
                {
                    mDirectoryFilter.IsEnabled = value;
                    OnPropertyChanged( "IsFilterByDirectoryEnabled" );
                }
            }

        }

        /// <summary>
        /// Sets or checks, whether filtering by file list is enabled.
        /// </summary>
        public bool IsFilterByFileListEnabled
        {
            get { return mIsFilterByFileListEnabled; }
            set
            {
                if( mIsFilterByFileListEnabled != value )
                {
                    mIsFilterByFileListEnabled = value;
                    OnPropertyChanged( "IsFilterByFileListEnabled" );
                }
            }

        }
    }
}
