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
using System.Windows.Shapes;

namespace CodeXCavator
{

    /// <summary>
    /// Interaction logic for SelectCreateIndexWindow.xaml
    /// </summary>
    public partial class SelectCreateIndexWindow : Window
    {
        public string IndexFileToOpen { get; set; }

        public SelectCreateIndexWindow()
        {
            InitializeComponent();

            //Now fill in some default to help users get started when they create a new index file.
            String path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            String indexFile = path + "\\MyIndexConfig.xml";
            String indexLocation = path + "\\MyIndex";

            XMLIndexFile.Text = indexFile;
            XMLIndexLocation.Text = indexLocation;
            FileSourceDirectoriesRecursive.IsChecked = true;
            FileSourceDirectoriesInclude.Text = "*.cs|*.resx|*.bas|*.bat|*.c|*.cls|*.com|*.cpp|*.cs|*.cxx|*.def|*.dsr|*.f|*.f90|*.fi|*.for|*.frm|*.h|*.hpp|*.htm|*.html|*.idl|*.inc|*.inl|*.mak|*.odl|*.rc|*.rgs|*.sql|*.vb|*.vbs|*.xml|*.xaml";
            FileSourceDirectories.Text = ""; //path;

            //Also show the last index opened.
            string lastIndexFile = CodeXCavator.UI.MRUHandler.GetLastMRUFile(0);
            if (lastIndexFile != null && lastIndexFile != "")
            {
                OpenMRU1Button.Content = "Open " + lastIndexFile;
            }
            else
            {
                OpenMRU1Button.Content = "(no recent file to show)";
                OpenMRU1Button.IsEnabled = false;
            }
            lastIndexFile = CodeXCavator.UI.MRUHandler.GetLastMRUFile(1);
            if (lastIndexFile != null && lastIndexFile != "")
            {
                OpenMRU2Button.Content = "Open " + lastIndexFile;
            }
            else
            {
                OpenMRU2Button.Content = "(no recent file to show)";
                OpenMRU2Button.IsEnabled = false;
            }
            lastIndexFile = CodeXCavator.UI.MRUHandler.GetLastMRUFile(2);
            if (lastIndexFile != null && lastIndexFile != "")
            {
                OpenMRU3Button.Content = "Open " + lastIndexFile;
            }
            else
            {
                OpenMRU3Button.Content = "(no recent file to show)";
                OpenMRU3Button.IsEnabled = false;
            }
        }

        private void OpenMRU(int whichMru)
        {
            string lastFile = CodeXCavator.UI.MRUHandler.GetLastMRUFile(whichMru);
            IndexFileToOpen = lastFile;
            CodeXCavator.UI.MRUHandler.PushMRUFile(lastFile);

            //Now close this form, returning to our set the index
            this.Close();
        }

        private void OpenMRU1Button_Click(object sender, RoutedEventArgs e)
        {
            OpenMRU(0);
        }

        private void OpenMRU2Button_Click(object sender, RoutedEventArgs e)
        {
            OpenMRU(1);
        }

        private void OpenMRU3Button_Click(object sender, RoutedEventArgs e)
        {
            OpenMRU(2);
        }

        private void BrowseExistingIndexFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog ofd = new System.Windows.Forms.OpenFileDialog();
            ofd.RestoreDirectory = true;
            ofd.Filter = "xml files (*.xml)|*.xml";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            ofd.ValidateNames = true;
            ofd.DefaultExt = "xml";
            ofd.Title = "Select Index File";
            ofd.FilterIndex = 1;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string indexFile = ofd.FileName;
                IndexFileToOpen = indexFile;
                CodeXCavator.UI.MRUHandler.PushMRUFile(indexFile);

                //Now close this form, returning our set index
                this.Close();
            }
        }

        private void BrowseNewIndexFile_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.SaveFileDialog ofd = new System.Windows.Forms.SaveFileDialog();
            ofd.RestoreDirectory = true;
            ofd.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
            ofd.CheckFileExists = false;
            ofd.CheckPathExists = true;
            ofd.CreatePrompt = false;
            ofd.OverwritePrompt = true;
            ofd.ValidateNames = true;
            ofd.DefaultExt = "xml";
            ofd.Title = "Select Index File";
            ofd.FilterIndex = 1;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string indexFile = ofd.FileName;
                XMLIndexFile.Text = indexFile;

            }
        }

        private void BrowseIndexContent_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
            ofd.Description = "Folder to Contain Index Files";
            ofd.ShowNewFolderButton = true;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string indexLocation = ofd.SelectedPath;
                XMLIndexLocation.Text = indexLocation;
            }
        }

        private void BrowseCodeDirs_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog ofd = new System.Windows.Forms.FolderBrowserDialog();
            ofd.Description = "Source Code Folder to Index";
            ofd.ShowNewFolderButton = false;
            if (ofd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string path = ofd.SelectedPath;
                String dirs = FileSourceDirectories.Text;
                if (dirs != String.Empty)
                {
                    dirs = dirs + Environment.NewLine;
                }
                dirs = dirs + path;
                FileSourceDirectories.Text = dirs;
            }
        }

        /// <summary>
        /// Create a basic configuration index file using the information provided.
        /// </summary>
        private void CreateIndexFile_Click(object sender, RoutedEventArgs e)
        {
            string indexFile = XMLIndexFile.Text;
            string indexLocation = XMLIndexLocation.Text;
            string fileSourceDirectories = FileSourceDirectories.Text; ;
            if (fileSourceDirectories == "")
            {
                MessageBox.Show("Please select at least one source code directory first!");
                return;
            }
            bool? recursive = FileSourceDirectoriesRecursive.IsChecked;
            string includes = FileSourceDirectoriesInclude.Text;

            bool xmlOk = false;
            try
            {
                System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(indexFile);
                xmlWriter.WriteStartDocument();
                xmlWriter.WriteStartElement("Index");
                xmlWriter.WriteAttributeString("Path", indexLocation);
                xmlWriter.WriteStartElement("FileSources");

                string[] delimiters = new string[] { ";", "\r\n" };
                string[] dirs = fileSourceDirectories.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < dirs.Length; i++)
                {
                    xmlWriter.WriteStartElement("Directory");
                    xmlWriter.WriteAttributeString("Path", dirs[i]);
                    if (recursive == true)
                    {
                        xmlWriter.WriteAttributeString("Recursive", "true");
                    }
                    if (recursive == false)
                    {
                        xmlWriter.WriteAttributeString("Recursive", "false");
                    }
                    xmlWriter.WriteAttributeString("Include", includes);
                    xmlWriter.WriteEndElement();
                }

                xmlWriter.WriteEndElement();
                xmlWriter.WriteEndElement();

                xmlWriter.WriteEndDocument();
                xmlWriter.Close();

                xmlOk = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            if (xmlOk)
            {
                xmlOk = CodeXCavator.UI.MRUHandler.UpdateIndexFile(indexFile);
            }

            if (xmlOk)
            {
                //Now close this form, returning set index
                CodeXCavator.UI.MRUHandler.PushMRUFile(indexFile);
                IndexFileToOpen = indexFile;
                this.Close();
            }
        }
    }
}
