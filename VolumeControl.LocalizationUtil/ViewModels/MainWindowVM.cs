using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using VolumeControl.LocalizationUtil.Helpers.Collections;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public class MainWindowVM : INotifyPropertyChanged
    {
        #region Constructor
        public MainWindowVM()
        {
#if DEBUG
            EnableDebugOptions = true;
#endif

            CreateNewSubNodeCommand = new(parameter =>
            {
                JsonObjectVM node;
                if (parameter is TranslationConfigVM configVM)
                {
                    node = configVM.RootNode;
                }
                else if (parameter is JsonObjectVM objectVM)
                {
                    node = objectVM;
                }
                else if (parameter is JsonValueVM) { return; }
                else throw new InvalidOperationException();

                var newSubNode = node.CreateSubNode(InitialSubNodeName);

                node.IsExpanded = true;
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
                    ((JsonObjectVM)node.Parent!).RemoveSubNode(node);
                }
            });
            DeleteValueCommand = new(parameter =>
            {
                var node = (JsonObjectVM)MainWindow.TreeView.SelectedItem;
                var value = (JsonNodeVM)parameter!;

                node.RemoveValue(value);
            });
            UnloadConfigCommand = new(parameter =>
            {
                var configVM = (TranslationConfigVM)parameter!;

                var index = TranslationConfigs.IndexOf(configVM);

                TranslationConfigs.RemoveAt(index);
            });
        }
        #endregion Constructor

        #region Properties
        private MainWindow MainWindow { get; set; } = null!;
        public ObservableImmutableList<TranslationConfigVM> TranslationConfigs { get; } = new();
        public FileLoaderVM FileLoader { get; } = new();
        public LogVM LogVM { get; } = new();
        public PathBoxVM PathBoxVM { get; } = new();
        public bool EnableDebugOptions { get; }
        public bool AllowAddMultipleValues { get; set; } = false;
        public bool AllowAddValuesAndSubNodes { get; set; } = false;
        public string InitialSubNodeName { get; set; } = "(New Node)";
        #endregion Properties

        #region Commands
        public CustomCommand CreateNewSubNodeCommand { get; }
        public CustomCommand CreateNewValueCommand { get; }
        public string NewValueContent { get; set; } = string.Empty;
        public CustomCommand DeleteNodeCommand { get; }
        public CustomCommand DeleteValueCommand { get; }
        public CustomCommand UnloadConfigCommand { get; }
        #endregion Commands

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        #region Methods
        internal void AttachToMainWindow(MainWindow mainWindow)
        {
            MainWindow = mainWindow;
            PathBoxVM.AttachToMainWindow(mainWindow);

            MainWindow.TreeView.SelectedItemChanged += this.TreeView_SelectedItemChanged;
        }
        #endregion Methods

        #region EventHandlers

        #region TreeView
        private void TreeView_SelectedItemChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<object> e)
        { // clear the new value string when the selected item is changed
            NewValueContent = string.Empty;
        }
        #endregion TreeView

        #endregion EventHandlers
    }
}
