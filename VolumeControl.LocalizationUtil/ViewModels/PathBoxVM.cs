using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using VolumeControl.LocalizationUtil.Helpers.Collections;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public class PathBoxVM : INotifyPropertyChanged
    {
        #region Fields
        private bool _isSettingPath = false;
        private bool _isAttachedToWindow = false;
        private MainWindow MainWindow = null!;
        #endregion Fields

        #region Properties
        private MainWindowVM MainWindowVM => MainWindow.VM;
        private TreeView TreeView => MainWindow.TreeView;
        private ObservableImmutableList<TranslationConfigVM> TranslationConfigs => MainWindowVM.TranslationConfigs;
        public string CurrentPath
        {
            get => _currentPath;
            set
            {
                _currentPath = value;

                if (!_isSettingPath)
                {
                    SetNodeFromPath(_currentPath);
                }

                NotifyPropertyChanged();
            }
        }
        private string _currentPath = string.Empty;
        #endregion Properties

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        #region Methods

        #region AttachToMainWindow
        public void AttachToMainWindow(MainWindow mainWindow)
        {
            if (_isAttachedToWindow)
                throw new InvalidOperationException("Already attached to main window!");
            _isAttachedToWindow = true;

            MainWindow = mainWindow;
            TreeView.SelectedItemChanged += TreeView_SelectedItemChanged;

            MainWindow.PathBox.SetBinding(TextBox.TextProperty, new Binding($"{nameof(MainWindowVM.PathBoxVM)}.{nameof(CurrentPath)}")
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
            });
        }
        #endregion AttachToMainWindow

        internal void DeselectAll(IEnumerable<TreeViewNodeVM> nodes, bool recurse = true)
        {
            foreach (var node in nodes)
            {
                node.IsSelected = false;
                if (recurse)
                {
                    if (node is JsonObjectVM objectVM)
                    {
                        DeselectAll(objectVM.SubNodes);
                        DeselectAll(objectVM.Values);
                    }
                    else if (node is TranslationConfigVM configVM)
                    {
                        configVM.RootNode.IsSelected = false;
                        DeselectAll(configVM.RootNode.SubNodes);
                        DeselectAll(configVM.RootNode.Values);
                    }
                }
            }
        }
        internal void CollapseAll(IEnumerable<TreeViewNodeVM> nodes, bool recurse = false)
        {
            // this method doesn't work correctly

            foreach (var node in nodes)
            {
                if (recurse)
                {
                    if (node is TranslationConfigVM configVM)
                    {
                        CollapseAll(configVM.RootNode.SubNodes, recurse);
                        continue;
                    }
                    else if (node is JsonObjectVM objectVM)
                    {
                        CollapseAll(objectVM.SubNodes, recurse);
                        continue;
                    }
                }

                node.IsExpanded = false;
            }
        }
        private void SetNodeFromPath(string path)
        {
            // this has a bug where you can only successfully set the selected node when its tree hasn't been opened yet.
            //  the treeview is undoing the changes after this method returns.

            if (path == null || path.Length == 0 || TreeView.Items.Count == 0)
                return;

            var names = path.Split(JsonNodeVM.NodePathSeparatorChar, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

            if (names.Length == 0) return;

            const StringComparison stringComparison = StringComparison.OrdinalIgnoreCase;

            DeselectAll(TranslationConfigs, true);

            // find the target translation config
            JsonObjectVM? node = null;
            int rootNodeIndex = 0;
            foreach (var translationConfigVM in TranslationConfigs)
            {
                if (translationConfigVM.FileName.Equals(names[0], stringComparison))
                {
                    if (names.Length == 1)
                    { // this is the target, select it & return
                        TreeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                        translationConfigVM.IsSelected = true;
                        TreeView.SelectedItemChanged += TreeView_SelectedItemChanged;
                        return;
                    }
                    else
                    { // the target is within this, expand it & break
                        translationConfigVM.IsExpanded = true;
                        node = translationConfigVM.RootNode;
                        break;
                    }
                }
                ++rootNodeIndex;
            }
            if (node == null) return; //< path root doesn't exist

            // enumerate the rest of the path
            for (int i = 1, i_max = names.Length, i_last = i_max - 1; i < i_max; ++i)
            {
                var name = names[i];

                if (node.FindSubNodeWithName(name, stringComparison) is JsonObjectVM subNode)
                {
                    node = subNode;
                    if (i == i_last)
                    {
                        TreeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                        node.IsSelected = true;
                        TreeView.SelectedItemChanged += TreeView_SelectedItemChanged;
                        return;
                    }
                    else
                    {
                        node.IsExpanded = true;
                    }
                }
                else if (node.FindValueWithName(name, stringComparison) is JsonNodeVM valueVM)
                { // select this because it's the closest node to the target; values do not have subnodes
                    TreeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                    valueVM.IsSelected = true;
                    TreeView.SelectedItemChanged += TreeView_SelectedItemChanged;

                    if (i != i_last)
                    { // the path still has more elements, remove them
                        _isSettingPath = true;
                        CurrentPath = string.Join(JsonNodeVM.NodePathSeparatorChar, names[..i]);
                        _isSettingPath = false;
                    }
                    return;
                }
                else break;
            }
        }
        #endregion Methods

        #region EventHandlers
        private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (_isSettingPath) return;

            if (e.NewValue == null)
            {
                _isSettingPath = true;
                CurrentPath = string.Empty;
                _isSettingPath = false;
            }
            else if (e.NewValue is JsonNodeVM nodeVM)
            {
                _isSettingPath = true;
                CurrentPath = nodeVM.GetNodePath();
                _isSettingPath = false;
            }
            else if (e.NewValue is TranslationConfigVM translationConfigVM)
            {
                _isSettingPath = true;
                CurrentPath = translationConfigVM.FileName;
                _isSettingPath = false;
            }
        }
        #endregion EventHandlers
    }
}
