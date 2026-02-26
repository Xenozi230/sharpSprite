using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;

namespace SharpSprite.App.Ui.Docking
{

    public abstract partial class DockPanelBase : ObservableObject, IDockPanel
    {
        public abstract string PanelId { get; }
        public abstract string DisplayName { get; }
        public abstract UserControl View { get; }
        public abstract DockPosition DockPosition { get; }
        public virtual double? PreferredWidth { get; } = null;
        public virtual double? PreferredHeight { get; } = null;
        public virtual bool IsClosable { get; } = true;

        [ObservableProperty]
        private bool _isVisible = true;
    }
}