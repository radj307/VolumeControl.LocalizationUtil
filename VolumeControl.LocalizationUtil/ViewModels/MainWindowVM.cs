using VolumeControl.LocalizationUtil.Helpers.Collections;

namespace VolumeControl.LocalizationUtil.ViewModels
{
    public class MainWindowVM
    {
        public MainWindowVM()
        {
#if DEBUG
            EnableDebugOptions = true;
#endif
        }

        #region Properties
        public ObservableImmutableList<TranslationConfigVM> TranslationConfigs { get; } = new();
        public FileLoaderVM FileLoader { get; } = new();
        public LogVM LogVM { get; } = new();
        public PathBoxVM PathBoxVM { get; } = new();
        public bool EnableDebugOptions { get; }
        #endregion Properties
    }
}
