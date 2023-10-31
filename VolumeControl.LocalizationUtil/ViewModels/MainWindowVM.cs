using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VolumeControl.LocalizationUtil.Helpers.Collections;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        public MainWindowVM()
        {
#if DEBUG
            EnableDebugOptions = true;
#endif

            CreateNewSubNodeCommand = new(parameter =>
            {
                var newSubNode = ((JsonObjectVM)parameter!).CreateSubNode("(New Node)");

                newSubNode.IsSelected = true;
            });
            CreateNewValueCommand = new(parameter =>
            {
                ((JsonObjectVM)parameter!).CreateValue(NewValueContent);
                NewValueContent = string.Empty;
            });
            DeleteNodeCommand = new(parameter =>
            {
                var node = (JsonObjectVM)parameter!;
                if (node.IsRootNode)
                {
                    throw new InvalidOperationException("Cannot delete the root node!");
                }
                else
                {
                    ((JsonObjectVM)node.Parent!).TryRemoveSubNode(node);
                }
            });
            DeleteValueCommand = new(parameter =>
            {
                var node = (JsonObjectVM)MainWindow.TreeView.SelectedItem;
                var value = (JsonNodeVM)parameter!;

                node.Values.Remove(value);
            });
        }

        #region Properties
        private MainWindow MainWindow { get; set; } = null!;
        public ObservableImmutableList<TranslationConfigVM> TranslationConfigs { get; } = new();
        public FileLoaderVM FileLoader { get; } = new();
        public LogVM LogVM { get; } = new();
        public PathBoxVM PathBoxVM { get; } = new();
        public bool EnableDebugOptions { get; }
        #endregion Properties

        #region Commands
        public CustomCommand CreateNewSubNodeCommand { get; }
        public CustomCommand CreateNewValueCommand { get; }
        public string NewValueContent { get; set; } = string.Empty;
        public CustomCommand DeleteNodeCommand { get; }
        public CustomCommand DeleteValueCommand { get; }
        #endregion Commands

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        internal void AttachToMainWindow(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            PathBoxVM.AttachToMainWindow(mainWindow);

            MainWindow.TreeView.SelectedItemChanged += this.TreeView_SelectedItemChanged;
        }

        private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        { // clear the new value string when the selected item is changed
            NewValueContent = string.Empty;
        }
    }
}
