using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using VolumeControl.LocalizationUtil.Helpers;
using VolumeControl.LocalizationUtil.ViewModels;
using VolumeControl.Log;

namespace VolumeControl.LocalizationUtil
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            VM = (MainWindowVM)FindResource(nameof(VM));
            VM.AttachToMainWindow(this);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
#if DEBUG
            // DEBUG SETUP
            var screenSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Size;
            Left = screenSize.Width + 10;
            Top = screenSize.Height - ActualHeight - 100;
            AddTestConfigsButton_Click(null, null);
#endif
        }
        #endregion Constructor

        #region Properties
        public bool UseRecursion { get; set; } = true;
        internal MainWindowVM VM { get; }
        #endregion Properties

        #region EventHandlers

        #region LoadFileButton
        private void LoadFileButton_Click(object sender, RoutedEventArgs e)
        {
            var filePath = VM.FileLoader.FilePath;
            if (VM.TranslationConfigs.Any(config => config.OriginalFilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase)))
            {
                FLog.Error($"Translation config \"{filePath}\" is already loaded!");
                return;
            }

            var content = VM.FileLoader.LoadFileContent();

            if (content == null)
            {
                FLog.Error($"Failed to load translation config file \"{filePath}\"!");
                return;
            }

            if (TranslationConfigVM.TryCreateInstance(filePath, content, out var translationConfigVM))
            {
                VM.TranslationConfigs.Add(translationConfigVM);
                VM.FileLoader.FilePath = string.Empty;
            }
        }
        #endregion LoadFileButton

        #region ChooseFileButton
        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
            => VM.FileLoader.ShowChooseFileDialog();
        #endregion ChooseFileButton

        #region AddTestConfigButton
        private void AddTestConfigsButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var fileName in LocalizationTestFiles.FileNames)
            {
                var content = LocalizationTestFiles.GetFileContent(fileName);

                if (content == null) continue;

                if (TranslationConfigVM.TryCreateInstance(fileName, content, out var translationConfigVM))
                {
                    VM.TranslationConfigs.Add(translationConfigVM);
                }
            }
        }
        #endregion AddTestConfigButton

        #region DeselectAllTreeViewItemsButton
        private void DeselectAllTreeViewItemsButton_Click(object sender, RoutedEventArgs e)
        {
            VM.PathBoxVM.DeselectAll(VM.TranslationConfigs, UseRecursion);
        }
        #endregion DeselectAllTreeViewItemsButton

        #region CollapseAllTreeViewItemsButton
        private void CollapseAllTreeViewItemsButton_Click(object sender, RoutedEventArgs e)
        {
            VM.PathBoxVM.CollapseAll(VM.TranslationConfigs, UseRecursion);
        }
        #endregion CollapseAllTreeViewItemsButton

        #region ConfigLoadButton
        private void ConfigLoadButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var configVM = (TranslationConfigVM)button.DataContext;
            configVM.ReloadFromFile();
        }
        #endregion ConfigLoadButton

        #region ConfigSaveButton
        private void ConfigSaveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var configVM = (TranslationConfigVM)button.DataContext;

            if (MessageBoxResult.OK == MessageBox.Show($"This will overwrite the original file:\n{configVM.OriginalFilePath}\nMake sure you're sure before you continue!", "Are you sure?", MessageBoxButton.OKCancel, MessageBoxImage.Question))
            {
                configVM.OverwriteFile();
            }
            else
            {
                MessageBox.Show("The original file was not overwritten.", "Cancelled");
            }
        }
        #endregion ConfigSaveButton

        #region ConfigLocaleIDTextBox
        private void ConfigLocaleIDTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((e.Key < Key.A || e.Key > Key.Z)
                && e.Key != Key.Left
                && e.Key != Key.Right
                && e.Key != Key.Back
                && e.Key != Key.LeftCtrl
                && e.Key != Key.RightCtrl)
                e.Handled = true;
        }
        #endregion ConfigLocaleIDTextBox

        #region SaveToNewFilePathButton
        private void SaveToNewFilePathButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var configVM = (TranslationConfigVM)button.DataContext;

            if (File.Exists(configVM.NewFilePath))
            {
                if (MessageBoxResult.Cancel == MessageBox.Show("A file already exists at this location, are you sure you want to overwrite it? (This cannot be undone!)", "Are you sure?", MessageBoxButton.OKCancel, MessageBoxImage.Exclamation))
                {
                    MessageBox.Show("The file was not overwritten.", "Cancelled");
                    return;
                }
            }

            if (configVM.SaveToFile())
            { // open the file in a text editor
                try
                {
                    Process.Start(new ProcessStartInfo(configVM.NewFilePath) { UseShellExecute = true })?.Dispose();
                }
                catch { }
            }
        }
        #endregion SaveToNewFilePathButton

        #endregion EventHandlers
    }
}
