using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public abstract class TreeViewNodeVM : INotifyPropertyChanged
    {
        #region Constructor
        protected TreeViewNodeVM(TreeViewNodeVM? parent)
        {
            ParentTreeViewNode = parent;
        }
        #endregion Constructor

        #region Properties
        public TreeViewNodeVM? ParentTreeViewNode { get; }
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (value == _isSelected) return;

                _isSelected = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isSelected;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (value == _isExpanded) return;

                _isExpanded = value;
                NotifyPropertyChanged();
            }
        }
        private bool _isExpanded;
        #endregion Properties

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events
    }
}
