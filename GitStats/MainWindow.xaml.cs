using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using GitStats.Properties;
using Forms = System.Windows.Forms;

namespace GitStats
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Prop changed
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        #endregion

        #region Notify props
        private string _pathToRepo;
        public string PathToRepo
        {
            get { return _pathToRepo; }
            set
            {
                _pathToRepo = value;
                OnPropertyChanged(nameof(PathToRepo));
            }
        }

        private StringCollection _savedPaths;
        public StringCollection SavedPaths
        {
            get { return _savedPaths; }
            set
            {
                _savedPaths = value;
                OnPropertyChanged(nameof(SavedPaths));
            }
        }
        #endregion

        public GitParsing.RepoStats Stats { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            SavedPaths = Settings.Default.SavedPaths;
            DataContext = this;
        }

        private void bLoadRepo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Stats = GitParsing.getRepoStats(PathToRepo);
                HandleSavedPaths();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load repository from given path");
            }

            ManuallyRefreshDataContext();
        }

        private void HandleSavedPaths()
        {
            if (Settings.Default.SavedPaths.Contains(PathToRepo))
                return;

            Settings.Default.SavedPaths.Add(PathToRepo);
            Settings.Default.Save();
            SavedPaths = Settings.Default.SavedPaths;
        }

        private void ManuallyRefreshDataContext()
        {
            DataContext = null;
            DataContext = this;
        }

        private void BOpenFolderPicker_OnClick(object sender, RoutedEventArgs e)
        {
            var folderDialog = new Forms.FolderBrowserDialog();
            var result = folderDialog.ShowDialog();
            if (result == Forms.DialogResult.OK)
                cbRepoPath.Text = folderDialog.SelectedPath;
        }
    }
}
