using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public class FileLoaderVM : INotifyPropertyChanged
    {
        #region Constructor
        public FileLoaderVM()
        {
            FilePath = string.Empty;
        }
        #endregion Constructor

        #region Properties
        public string FilePath
        {
            get => _filePath;
            set
            {
                var incomingPath = value.Trim();

                if (incomingPath.Length > 0)
                {
                    try
                    {
                        _filePath = Path.GetFullPath(incomingPath);
                    }
                    catch
                    {
                        _filePath = incomingPath;
                    }
                }
                else _filePath = incomingPath;

                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(FileExists));
            }
        }
        private string _filePath = string.Empty;
        public bool FileExists => FilePath.Length > 0 && File.Exists(FilePath);
        #endregion Properties

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        #region Methods
        public string? LoadFileContent()
        {
            if (FilePath.Length == 0 || !FileExists) return null;

            return File.ReadAllText(FilePath);
        }
        public void ShowChooseFileDialog()
        {
            var dialog = new CommonOpenFileDialog()
            {
                Title = "Select Translation Config File",
                DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\radj307\\VolumeControl\\Localization",
                DefaultExtension = ".json",
                IsFolderPicker = false,
                EnsureFileExists = true,
                EnsurePathExists = true,
                EnsureValidNames = true,
                Multiselect = false,
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                FilePath = dialog.FileName ?? string.Empty;
            }
        }
        #endregion Methods
    }
}
