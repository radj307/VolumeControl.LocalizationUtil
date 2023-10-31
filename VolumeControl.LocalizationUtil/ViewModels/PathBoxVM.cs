using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VolumeControl.LocalizationUtil.Helpers.Collections;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public static class TreeViewExtensions
    {
        /// <summary>
        /// Recursively search for an item in this subtree.
        /// </summary>
        /// <param name="container">
        /// The parent ItemsControl. This can be a TreeView or a TreeViewItem.
        /// </param>
        /// <param name="item">
        /// The item to search for.
        /// </param>
        /// <returns>
        /// The TreeViewItem that contains the specified item.
        /// </returns>
        public static TreeViewItem GetTreeViewItem(ItemsControl container, object item)
        {
            if (container != null)
            {
                if (container.DataContext == item)
                {
                    return container as TreeViewItem;
                }

                // Expand the current container
                if (container is TreeViewItem && !((TreeViewItem)container).IsExpanded)
                {
                    container.SetValue(TreeViewItem.IsExpandedProperty, true);
                }

                // Try to generate the ItemsPresenter and the ItemsPanel.
                // by calling ApplyTemplate.  Note that in the
                // virtualizing case even if the item is marked
                // expanded we still need to do this step in order to
                // regenerate the visuals because they may have been virtualized away.

                container.ApplyTemplate();
                ItemsPresenter itemsPresenter =
                    (ItemsPresenter)container.Template.FindName("ItemsHost", container);
                if (itemsPresenter != null)
                {
                    itemsPresenter.ApplyTemplate();
                }
                else
                {
                    // The Tree template has not named the ItemsPresenter,
                    // so walk the descendents and find the child.
                    itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    if (itemsPresenter == null)
                    {
                        container.UpdateLayout();

                        itemsPresenter = FindVisualChild<ItemsPresenter>(container);
                    }
                }

                Panel itemsHostPanel = (Panel)VisualTreeHelper.GetChild(itemsPresenter, 0);

                // Ensure that the generator for this panel has been created.
                UIElementCollection children = itemsHostPanel.Children;

                VirtualizingStackPanel virtualizingPanel =
                    itemsHostPanel as VirtualizingStackPanel;

                for (int i = 0, count = container.Items.Count; i < count; i++)
                {
                    TreeViewItem subContainer;
                    if (virtualizingPanel != null)
                    {
                        // Bring the item into view so
                        // that the container will be generated.
                        virtualizingPanel.BringIntoView();

                        subContainer =
                            (TreeViewItem)container.ItemContainerGenerator.
                            ContainerFromIndex(i);
                    }
                    else
                    {
                        subContainer =
                            (TreeViewItem)container.ItemContainerGenerator.
                            ContainerFromIndex(i);

                        // Bring the item into view to maintain the
                        // same behavior as with a virtualizing panel.
                        subContainer.BringIntoView();
                    }

                    if (subContainer != null)
                    {
                        // Search the next level for the object.
                        TreeViewItem resultContainer = GetTreeViewItem(subContainer, item);
                        if (resultContainer != null)
                        {
                            return resultContainer;
                        }
                        else
                        {
                            // The object is not under this TreeViewItem
                            // so collapse it.
                            subContainer.IsExpanded = false;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Search for an element of a certain type in the visual tree.
        /// </summary>
        /// <typeparam name="T">The type of element to find.</typeparam>
        /// <param name="visual">The parent element.</param>
        /// <returns></returns>
        private static T FindVisualChild<T>(Visual visual) where T : Visual
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(visual); i++)
            {
                Visual child = (Visual)VisualTreeHelper.GetChild(visual, i);
                if (child != null)
                {
                    T correctlyTyped = child as T;
                    if (correctlyTyped != null)
                    {
                        return correctlyTyped;
                    }

                    T descendent = FindVisualChild<T>(child);
                    if (descendent != null)
                    {
                        return descendent;
                    }
                }
            }

            return null;
        }

        public static void SelectTreeViewItem(this TreeView treeView, object itemToSelect)
        {
            TreeViewItem item = FindTreeViewItem(treeView, itemToSelect);
            if (item != null)
            {
                item.IsSelected = true;
            }
        }

        private static TreeViewItem FindTreeViewItem(ItemsControl parent, object itemToFind)
        {
            if (parent == null) return null;

            // Check if the current item is a match
            if (parent.DataContext == itemToFind)
            {
                return parent as TreeViewItem;
            }

            // Search the children
            TreeViewItem result = null;
            for (int i = 0; i < parent.Items.Count; i++)
            {
                var childItem = parent.ItemContainerGenerator.ContainerFromIndex(i) as TreeViewItem;
                result = FindTreeViewItem(childItem, itemToFind);
                if (result != null)
                {
                    break;
                }
            }

            return result;
        }
    }
    public class PathBoxVM : INotifyPropertyChanged
    {
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

        private bool _isSettingPath = false;
        private bool _isAttachedToWindow = false;
        private MainWindow MainWindow = null!;

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

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

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
